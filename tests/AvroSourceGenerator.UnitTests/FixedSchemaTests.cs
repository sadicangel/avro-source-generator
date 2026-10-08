using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.UnitTests;

public sealed class FixedSchemaTests
{
    [Theory]
    [InlineData("Hash", null, "Hash", null, false)]
    [InlineData("Hash", "Demo", "Hash", "Demo", false)]
    [InlineData("class", "namespace.event", "@class", "@namespace.@event", false)]
    [InlineData("Hash", "Demo.event.Models", "Hash", "Demo.@event.Models", false)]
    [InlineData("Hash", null, "byte[]", null, true)]
    [InlineData("Hash", "Demo", "Hash", "Other", true)]
    [InlineData("Hash", null, "Other", null, true)]
    [InlineData("Hash", "namespace.event", "Hash", "@namespace.Other", true)]
    [InlineData("Hash", "namespace.event", "Hash", "@namespace", true)]
    [InlineData("Hash", "namespace", "Hash", "@namespace.@event", true)]
    [InlineData("Hash", null, "Hash", "@namespace", true)]
    [InlineData("Hash", "Demo", "Hash", null, true)]
    public void Substitution_ignores_keyword_escaping_but_detects_name_or_namespace_changes(
        string name, string? schemaNamespace, string csharpName, string? csharpNamespace, bool substituted)
    {
        var schema = new FixedSchema(new SchemaName(name, schemaNamespace), null, [], 16, ImmutableSortedDictionary<string, JsonElement>.Empty)
        {
            CSharpName = new CSharpName(csharpName, csharpNamespace)
        };

        Assert.Equal(substituted, schema.IsSubstituted);
    }

    [Fact]
    public void Keyword_escaping_keeps_fixed_declarations_in_rendered_variants()
    {
        var source = JsonNode.Parse(VariantGenerationAssertions.Schema("error", "fixed"))!;
        source["namespace"] = "namespace.event";
        source["fields"]![0]!["type"]![1]!["name"] = "class";
        var compiled = SchemaCompilerTestHelpers.Compile(
            GenerationTarget.Apache, ReferenceResolution.Strict, DuplicateResolution.Error,
            ("keywords.avsc", source.ToJsonString()));
        Assert.True(compiled.Compilation.IsValid);
        var fixedSchema = Assert.Single(compiled.RenderableFiles[0].EmittedSchemas.OfType<FixedSchema>());
        var variant = Assert.Single(compiled.RenderableFiles[0].EmittedSchemas.OfType<UnionObjectSchema>());

        Assert.False(fixedSchema.IsSubstituted);
        Assert.Equal(new CSharpName("@class", "@namespace.@event"), fixedSchema.CSharpName);
        Assert.Equal(variant.CSharpName, fixedSchema.InheritsFrom);
        Assert.Contains(variant.MemberSchemas, member => ReferenceEquals(fixedSchema, member));
    }
}
