using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class VariantSchemaTests
{
    [Theory]
    [InlineData(GenerationTarget.Modern, "record", "error", true)]
    [InlineData(GenerationTarget.Legacy, "error", "error", true)]
    [InlineData(GenerationTarget.Chr, "error", "record", true)]
    [InlineData(GenerationTarget.Apache, "error", "fixed", true)]
    [InlineData(GenerationTarget.Apache, "fixed", "fixed", true)]
    [InlineData(GenerationTarget.Modern, "error", "fixed", false)]
    [InlineData(GenerationTarget.Legacy, "record", "fixed", false)]
    [InlineData(GenerationTarget.Chr, "fixed", "fixed", false)]
    [InlineData(GenerationTarget.Apache, "error", "enum", false)]
    public void Only_generated_named_members_implement_variants(
        GenerationTarget target, string firstType, string secondType, bool supportsVariant)
    {
        var file = SchemaCompilerTestHelpers.ParseJson(VariantGenerationAssertions.Schema(firstType, secondType), target);
        Assert.True(file.IsValid);
        var container = Assert.IsType<RecordSchema>(file.RootSchema);
        var field = Assert.Single(container.Fields);
        var union = Assert.IsType<UnionSchema>(field.Type);

        Assert.Equal(supportsVariant, union.SupportsVariant());
        if (supportsVariant)
        {
            var variant = Assert.IsType<VariantSchema>(field.UnderlyingType);
            Assert.Equal("IEnvelopeChoiceVariant", variant.SchemaName.Name);
            Assert.Same(variant, union.UnderlyingSchema);
            Assert.Equal(variant.CSharpName, field.Type.CSharpName);
            Assert.Equal(variant.Documentation, field.Remarks);
            for (var i = 0; i < 2; i++)
            {
                var member = Assert.IsAssignableFrom<NamedSchema>(union.Schemas[i]);
                Assert.Same(member, variant.DerivedSchemas[i]);
                Assert.Same(member, file.Declarations.Single(schema => schema.SchemaName == member.SchemaName));
                Assert.Equal(variant.CSharpName, member.InheritsFrom);
                Assert.Contains(member.SchemaName, file.Dependencies[container.SchemaName]);
            }
        }
        else
        {
            Assert.DoesNotContain(file.Declarations, schema => schema is VariantSchema);
            Assert.All(file.Declarations.OfType<NamedSchema>(), schema => Assert.Null(schema.InheritsFrom));
        }
    }

    [Theory]
    [InlineData("record")]
    [InlineData("error")]
    [InlineData("fixed")]
    public void Single_members_and_optional_members_do_not_need_variants(string type)
    {
        var file = SchemaCompilerTestHelpers.ParseJson(VariantGenerationAssertions.Schema(type, "error"), GenerationTarget.Apache);
        var member = file.Declarations.OfType<NamedSchema>().First();

        Assert.False(UnionSchema.Create([], true).SupportsVariant());
        Assert.False(UnionSchema.Create([member], true).SupportsVariant());
        Assert.False(UnionSchema.Create([AvroSchema.Null, member], true).SupportsVariant());
        Assert.False(UnionSchema.Create([member, AvroSchema.Null], true).SupportsVariant());
        Assert.False(UnionSchema.Create([AvroSchema.Null, AvroSchema.Null, AvroSchema.Null], true).SupportsVariant());
        Assert.False(UnionSchema.Create([member, AvroSchema.String], true).SupportsVariant());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Nullable_variant_preserves_members_and_respects_nullable_options(bool nullFirst, bool useNullableReferenceTypes)
    {
        var schema = JsonNode.Parse(VariantGenerationAssertions.Schema("error", "fixed"))!;
        var members = schema["fields"]![0]!["type"]!.AsArray();
        if (nullFirst) members.Insert(0, "null");
        else members.Add("null");
        var file = SchemaCompilerTestHelpers.ParseJson(schema.ToJsonString(), GenerationTarget.Apache, useNullableReferenceTypes);
        Assert.True(file.IsValid);
        var field = Assert.Single(Assert.IsType<RecordSchema>(file.RootSchema).Fields);
        var union = Assert.IsType<UnionSchema>(field.Type);
        var variant = Assert.IsType<VariantSchema>(field.UnderlyingType);

        Assert.Equal(useNullableReferenceTypes, union.CSharpName.HasNullableAnnotation);
        Assert.Same(AvroSchema.Null, variant.DerivedSchemas[nullFirst ? 0 : 2]);
        Assert.All(variant.DerivedSchemas.OfType<NamedSchema>(), member => Assert.Equal(variant.CSharpName, member.InheritsFrom));
    }

    [Theory]
    [InlineData("record", DuplicateResolution.Error)]
    [InlineData("record", DuplicateResolution.Ignore)]
    [InlineData("error", DuplicateResolution.Error)]
    [InlineData("fixed", DuplicateResolution.Error)]
    public void Duplicate_union_declarations_report_the_second_source_instead_of_throwing(string type, DuplicateResolution resolution)
    {
        var source = JsonNode.Parse(VariantGenerationAssertions.Schema(type, type))!;
        source["fields"]![0]!["type"]![1]!["name"] = "First";
        var compiled = SchemaCompilerTestHelpers.Compile(
            GenerationTarget.Apache, ReferenceResolution.Strict, resolution,
            ("duplicates.avsc", source.ToJsonString()));

        var diagnostic = Assert.Single(compiled.Compilation.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.DuplicateSchema, diagnostic.Code);
        Assert.Equal(compiled.Files[0].DeclarationSpans[1], diagnostic.SourceSpan);
        Assert.Equal("duplicates.avsc", diagnostic.SourceSpan.SourceText.Path.ToString());
        Assert.False(compiled.Compilation.IsValid);
        Assert.Empty(compiled.RenderableFiles[0].EmittedSchemas);
    }

    [Fact]
    public void Enum_cannot_implement_a_variant_interface()
    {
        var schema = new EnumSchema(new SchemaName("Choice"), null, [], ["A"], null, ImmutableSortedDictionary<string, JsonElement>.Empty);

        Assert.Throws<InvalidOperationException>(() => schema with { InheritsFrom = new CSharpName("IVariant") });
        Assert.Null(schema.InheritsFrom);
    }
}
