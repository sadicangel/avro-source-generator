using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests;

public sealed class UnionGenerationCachingTests
{
    [Theory]
    [InlineData(LanguageFeatures.CSharp14)]
    [InlineData(LanguageFeatures.CSharp15)]
    public void Adding_null_refreshes_field_documentation_even_when_generated_members_are_unchanged(LanguageFeatures features)
    {
        var config = new ProjectConfig(features.HasUnions ? LanguageVersion.Preview : LanguageVersion.CSharp14)
        {
            AvroLibrary = "None",
            LanguageFeatures = features.ToString(),
            NativeUnions = features.HasUnions ? "true" : "false",
        };
        var initial = GeneratorInput.Create([ProjectFile.Schema(VariantGenerationAssertions.Schema("record", "record"), "envelope.avsc")], [], config);
        var changed = GeneratorInput.Create([ProjectFile.Schema(VariantGenerationAssertions.Schema("record", "record", nullable: true), "envelope.avsc")], [], config);
        var driver = initial.GeneratorDriver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("<see langword=\"null\"/>", string.Join("\n", driver.GetRunResult().GeneratedTrees));

        driver = driver.ReplaceAdditionalText(initial.AdditionalTexts[0], changed.AdditionalTexts[0])
            .RunGeneratorsAndUpdateCompilation(initial.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
        var source = string.Join("\n", driver.GetRunResult().GeneratedTrees);
        Assert.Contains("<see langword=\"null\"/>", source);
        Assert.Contains(features.HasUnions ? "EnvelopeChoiceUnion? choice" : "IEnvelopeChoiceVariant? choice", source);

        driver = driver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["RenderedFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    [Theory]
    [InlineData(LanguageFeatures.CSharp14)]
    [InlineData(LanguageFeatures.CSharp15)]
    public void Changing_union_members_refreshes_output_and_then_caches_it(LanguageFeatures features)
    {
        var schema = VariantGenerationAssertions.Schema("record", "record");
        var config = new ProjectConfig(features.HasUnions ? LanguageVersion.Preview : LanguageVersion.CSharp14)
        {
            AvroLibrary = "None",
            LanguageFeatures = features.ToString(),
            NativeUnions = features.HasUnions ? "true" : "false",
        };
        var initial = GeneratorInput.Create([ProjectFile.Schema(schema, "envelope.avsc")], [], config);
        var changed = GeneratorInput.Create([ProjectFile.Schema(schema.Replace("\"Second\"", "\"Third\""), "envelope.avsc")], [], config);
        var driver = initial.GeneratorDriver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken)
            .ReplaceAdditionalText(initial.AdditionalTexts[0], changed.AdditionalTexts[0])
            .RunGeneratorsAndUpdateCompilation(initial.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Null(compilation.GetTypeByMetadataName("Variants.Second"));
        Assert.NotNull(compilation.GetTypeByMetadataName("Variants.Third"));
        var unionSource = driver.GetRunResult().Results.Single().GeneratedSources.Single(source =>
            source.HintName == (features.HasUnions
                ? "Variants.EnvelopeChoiceUnion.Avro.g.cs"
                : "Variants.IEnvelopeChoiceVariant.Avro.g.cs")).SourceText.ToString();
        Assert.Contains("Third", unionSource);
        Assert.DoesNotContain("Second", unionSource);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["AvroFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));

        driver = driver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["RenderedFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Native_collection_unions_follow_the_opt_in_and_cache_branch_edits(bool useIdl, bool useNativeUnions)
    {
        const string json = """
            {
              "type": "record", "name": "Envelope", "namespace": "Collections",
              "fields": [
                { "name": "Content", "type": { "type": "array", "items": ["null", "int", "string"] } },
                { "name": "Kind", "type": ["boolean", "double"] },
                { "name": "Counts", "type": { "type": "map", "values": ["null", "int"] } }
              ]
            }
            """;
        const string idl = """
            namespace Collections;
            schema Envelope;
            record Envelope {
                array<union { null, int, string }> Content;
                union { boolean, double } Kind;
                map<union { null, int }> Counts;
            }
            """;
        var config = new ProjectConfig(LanguageVersion.Preview)
        {
            AvroLibrary = "None",
            LanguageFeatures = "CSharp15",
            NativeUnions = useNativeUnions ? "true" : "false",
        };
        var source = useIdl ? ProjectFile.Source(idl, "envelope.avdl") : ProjectFile.Schema(json, "envelope.avsc");
        var changed = source with { Content = source.Content.Replace("string", "long", StringComparison.Ordinal) };
        var initial = GeneratorInput.Create([source], [], config);
        var updated = GeneratorInput.Create([changed], [], config);
        var cancellationToken = TestContext.Current.CancellationToken;
        var driver = initial.GeneratorDriver.RunGeneratorsAndUpdateCompilation(initial.Compilation,
            out var compilation, out var diagnostics, cancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(cancellationToken).Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));
        var recordSource = driver.GetRunResult().GeneratedTrees.Single(tree => tree.FilePath.EndsWith("Collections.Envelope.Avro.g.cs", StringComparison.Ordinal)).ToString();
        Assert.Contains(useNativeUnions ? "List<global::Collections.EnvelopeContentItemUnion?> Content" : "List<object?> Content", recordSource);
        Assert.Contains(useNativeUnions ? "EnvelopeKindUnion Kind" : "object Kind", recordSource);
        Assert.Contains("Dictionary<string, int?> Counts", recordSource);

        driver = driver.ReplaceAdditionalText(initial.AdditionalTexts[0], updated.AdditionalTexts[0])
            .RunGeneratorsAndUpdateCompilation(initial.Compilation, out compilation, out diagnostics, cancellationToken);
        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(cancellationToken).Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));
        if (useNativeUnions)
        {
            var generatedUnion = driver.GetRunResult().GeneratedTrees.Single(tree =>
                tree.FilePath.EndsWith("EnvelopeContentItemUnion.Avro.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("long", generatedUnion);
            Assert.DoesNotContain("string", generatedUnion);
        }

        driver = driver.RunGenerators(initial.Compilation, cancellationToken);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["RenderedFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }
}
