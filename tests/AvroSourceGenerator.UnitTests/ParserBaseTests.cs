using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests;

public sealed class ParserBaseTests
{
    [Fact]
    public void References_use_the_latest_declaration_and_keep_declaration_order()
    {
        var file = Parse("""
            schema Container;
            record Shared { string first; }
            record Shared { long latest; }
            record Container { Shared value; }
            """);

        Assert.True(file.IsValid);
        Assert.Equal(["Shared", "Shared", "Container"], file.Declarations.Select(schema => schema.SchemaName.Name));
        var latest = Assert.IsType<RecordSchema>(file.Declarations[1]);
        Assert.Equal("latest", Assert.Single(latest.Fields).Name.SchemaName);
        var container = Assert.IsType<RecordSchema>(file.Declarations[2]);
        Assert.Equal(latest.CSharpName, Assert.Single(container.Fields).Type.CSharpName);
        Assert.Empty(file.References);
    }

    [Fact]
    public void Variant_replacement_preserves_duplicate_declarations()
    {
        var file = AvroFile.Parse(
            new SourceText("test.avsc", """
                {"type":"record","name":"Container","fields":[
                    {"name":"first","type":{"type":"record","name":"Shared","fields":[{"name":"first","type":"string"}]}},
                    {"name":"choice","type":[
                        {"type":"record","name":"Shared","fields":[{"name":"latest","type":"long"}]},
                        {"type":"record","name":"Other","fields":[]}
                    ]},
                    {"name":"value","type":"Shared"}
                ]}
                """),
            new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.NullableReferenceTypes),
            TestContext.Current.CancellationToken);

        Assert.True(file.IsValid);
        var shared = file.Declarations.OfType<RecordSchema>().Where(schema => schema.SchemaName.Name == "Shared").ToArray();
        Assert.Equal(2, shared.Length);
        Assert.Null(shared[0].InheritsFrom);
        Assert.NotNull(shared[1].InheritsFrom);
        var container = file.Declarations.OfType<RecordSchema>().Single(schema => schema.SchemaName.Name == "Container");
        Assert.Equal(shared[1].CSharpName, container.Fields[2].Type.CSharpName);
    }

    [Fact]
    public void Recursive_references_record_dependencies_without_external_references()
    {
        var file = Parse("schema Node; record Node { Node next; }");

        Assert.True(file.IsValid);
        var node = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        Assert.Equal(node.SchemaName, Assert.Single(node.Fields).Type.SchemaName);
        Assert.Empty(file.References);
        Assert.Equal([node.SchemaName], file.Dependencies[node.SchemaName]);
    }

    private static AvroFile Parse(string text) => AvroFile.Parse(
        new SourceText("test.avdl", text),
        new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.NullableReferenceTypes),
        TestContext.Current.CancellationToken);
}
