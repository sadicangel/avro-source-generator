using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests;

public sealed class UnionTaggedSchemaTests
{
    [Theory]
    [InlineData(LanguageVersion.CSharp13, null, "Unions", false)]
    [InlineData(LanguageVersion.CSharp14, null, "Unions", false)]
    [InlineData(LanguageVersion.CSharp14, "CSharp15", "Unions", false)]
    [InlineData(LanguageVersion.CSharp8, "Latest", "Unions", false)]
    [InlineData((LanguageVersion)1500, null, null, false)]
    [InlineData((LanguageVersion)1500, null, "None", false)]
    [InlineData((LanguageVersion)1500, null, "invalid", false)]
    [InlineData((LanguageVersion)1500, null, "Unions", true)]
    [InlineData((LanguageVersion)1500, null, "unions", true)]
    [InlineData(LanguageVersion.Preview, null, null, false)]
    [InlineData(LanguageVersion.Preview, null, "Unions", true)]
    [InlineData(LanguageVersion.Preview, "CSharp14", "Unions", false)]
    [InlineData(LanguageVersion.Preview, "CSharp15", null, false)]
    [InlineData(LanguageVersion.Preview, "CSharp15", "Unions", true)]
    [InlineData(LanguageVersion.Preview, "Latest", null, false)]
    public void Tagged_unions_require_language_support_and_explicit_preview_opt_in(
        LanguageVersion languageVersion, string? languageFeatures, string? previewFeatures, bool tagged)
    {
        var config = new ProjectConfig(languageVersion >= (LanguageVersion)1500 ? LanguageVersion.Preview : languageVersion) { AvroLibrary = "None" };
        if (languageFeatures is not null) config.LanguageFeatures = languageFeatures;
        if (previewFeatures is not null) config.PreviewFeatures = previewFeatures;
        var input = GeneratorInput.Create([], [], config);
        var properties = ProjectProperties.FromAnalyzerOptions(input.OptionsProvider, TestContext.Current.CancellationToken);
        var configuration = GeneratorConfiguration.Resolve((
            properties,
            new CompilationEnvironment([], languageVersion)), TestContext.Current.CancellationToken);

        Assert.Equal(tagged, configuration.LanguageFeatures.HasUnions);
    }

    [Fact]
    public void Binding_imported_members_preserves_the_tagged_union_and_avro_order()
    {
        var options = new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp15);
        var member = AvroFile.Parse(new SourceText("id.avsc",
            """{"type":"fixed","name":"Id","namespace":"Demo","size":16}"""), options, TestContext.Current.CancellationToken);
        var envelope = AvroFile.Parse(new SourceText("envelope.avsc",
            """
            {
              "type":"record","name":"Envelope","namespace":"Demo",
              "fields":[{"name":"choice","type":["null","Id","string","bytes"]}]
            }
            """), options, TestContext.Current.CancellationToken);
        var symbols = SymbolTable.FromFiles([member, envelope], TestContext.Current.CancellationToken);
        var bound = BoundAvroFile.Bind(
            LinkedAvroFile.Link(envelope, symbols, TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);
        var field = Assert.Single(Assert.IsType<RecordSchema>(bound.Declarations.Single(schema => schema.SchemaName.Name == "Envelope")).Fields);
        var union = Assert.IsType<UnionSchema>(field.Type);
        var tagged = Assert.IsType<UnionTaggedSchema>(field.UnderlyingType);

        Assert.True(field.AllowsNull);
        Assert.Equal("global::Demo.EnvelopeChoiceUnion?", field.Type.CSharpName.FullName);
        Assert.Same(tagged, union.UnderlyingSchema);
        Assert.Equal([SchemaType.Null, SchemaType.Reference, SchemaType.String, SchemaType.Bytes], union.Schemas.Select(schema => schema.Type));
        Assert.Equal(AvroSchema.Bytes.CSharpName, union.Schemas[1].CSharpName);
        Assert.Contains(tagged.MemberSchemas, schema => schema.CSharpName == AvroSchema.Bytes.CSharpName);
        Assert.Equal([CSharpName.ByteArray, CSharpName.String], tagged.MemberSchemas.Select(schema => schema.CSharpName));
        Assert.Same(tagged, bound.Declarations.Single(schema => schema is UnionTaggedSchema));
    }

    [Fact]
    public void Binding_imported_members_reorders_tagged_members_by_resolved_csharp_names()
    {
        var options = new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp15);
        var fixedSchema = AvroFile.Parse(new SourceText("fixed.avsc",
            """{"type":"fixed","name":"ZFixed","namespace":"Demo","size":16}"""), options, TestContext.Current.CancellationToken);
        var enumSchema = AvroFile.Parse(new SourceText("enum.avsc",
            """{"type":"enum","name":"AEnum","namespace":"Demo","symbols":["A"]}"""), options, TestContext.Current.CancellationToken);
        var envelope = AvroFile.Parse(new SourceText("envelope.avsc",
            """
            {
              "type":"record","name":"Envelope","namespace":"Demo",
              "fields":[{"name":"choice","type":["AEnum","ZFixed"]}]
            }
            """), options, TestContext.Current.CancellationToken);
        var parsedField = Assert.Single(Assert.IsType<RecordSchema>(envelope.RootSchema).Fields);
        var parsedUnion = Assert.IsType<UnionTaggedSchema>(parsedField.UnderlyingType);
        Assert.Equal(["AEnum", "ZFixed"], parsedUnion.MemberSchemas.Select(schema => schema.SchemaName.Name));

        var symbols = SymbolTable.FromFiles([fixedSchema, enumSchema, envelope], TestContext.Current.CancellationToken);
        var bound = BoundAvroFile.Bind(LinkedAvroFile.Link(envelope, symbols, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        var field = Assert.Single(Assert.IsType<RecordSchema>(bound.RootSchema).Fields);
        var tagged = Assert.IsType<UnionTaggedSchema>(field.UnderlyingType);

        Assert.Equal(["ZFixed", "AEnum"], tagged.MemberSchemas.Select(schema => schema.SchemaName.Name));
        Assert.Equal([CSharpName.ByteArray, new CSharpName("AEnum", "Demo")], tagged.MemberSchemas.Select(schema => schema.CSharpName));
        Assert.Equal(["AEnum", "ZFixed"], Assert.IsType<UnionSchema>(field.Type).Schemas.Select(schema => schema.SchemaName.Name));
    }

    [Theory]
    [InlineData(LanguageFeatures.CSharp15)]
    [InlineData(LanguageFeatures.Unions)]
    public void Optional_tagged_unions_are_nullable_value_types_even_without_nullable_references(LanguageFeatures features)
    {
        var file = AvroFile.Parse(new SourceText("envelope.avsc",
            """
            {"type":"record","name":"Envelope","fields":[{"name":"choice","type":["null","string","int"]}]}
            """), new AvroParseOptions(GenerationTarget.Modern, features), TestContext.Current.CancellationToken);
        var field = Assert.Single(Assert.IsType<RecordSchema>(file.RootSchema).Fields);

        Assert.True(field.AllowsNull);
        Assert.True(field.Type.CSharpName.HasNullableAnnotation);
        Assert.IsType<UnionTaggedSchema>(field.UnderlyingType);
    }

    [Fact]
    public void Substituted_fixed_members_share_one_tagged_case_type()
    {
        var file = AvroFile.Parse(new SourceText("envelope.avsc",
            """
            {
              "type":"record","name":"Envelope",
              "fields":[{"name":"choice","type":[
                {"type":"fixed","name":"First","size":16},
                {"type":"fixed","name":"Second","size":16},
                "string"
              ]}]
            }
            """),
            new AvroParseOptions(GenerationTarget.Chr, LanguageFeatures.CSharp15), TestContext.Current.CancellationToken);
        var field = Assert.Single(Assert.IsType<RecordSchema>(file.RootSchema).Fields);
        var tagged = Assert.IsType<UnionTaggedSchema>(field.UnderlyingType);

        Assert.Equal(3, Assert.IsType<UnionSchema>(field.Type).Schemas.Length);
        Assert.Equal([CSharpName.String, CSharpName.ByteArray], tagged.MemberSchemas.Select(schema => schema.CSharpName));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Changing_preview_opt_in_invalidates_parsing_and_caches_the_new_output(bool taggedInitially)
    {
        var files = new[] { ProjectFile.Schema(VariantGenerationAssertions.Schema("record", "record")) }.ToImmutableArray();
        var initial = GeneratorInput.Create(files, [], new ProjectConfig(LanguageVersion.Preview)
        {
            AvroLibrary = "None",
            LanguageFeatures = "CSharp15",
            PreviewFeatures = taggedInitially ? "Unions" : "None",
        });
        var changed = GeneratorInput.Create(files, [], new ProjectConfig(LanguageVersion.Preview)
        {
            AvroLibrary = "None",
            LanguageFeatures = "CSharp15",
            PreviewFeatures = taggedInitially ? "None" : "Unions",
        });
        var driver = initial.GeneratorDriver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken)
            .WithUpdatedAnalyzerConfigOptions(changed.OptionsProvider)
            .RunGeneratorsAndUpdateCompilation(initial.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
        var source = string.Join("\n", driver.GetRunResult().GeneratedTrees);
        Assert.Contains(taggedInitially ? "interface IEnvelopeChoiceVariant" : "union EnvelopeChoiceUnion", source);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["AvroFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));

        driver = driver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["RenderedFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }
}
