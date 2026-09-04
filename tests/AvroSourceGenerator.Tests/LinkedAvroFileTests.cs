using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests;

public sealed class LinkedAvroFileTests
{
    [Fact]
    public void Projects_only_references_declared_by_the_file()
    {
        var shared = Parse("shared.avsc", Record("Shared"));
        var unrelated = Parse("unrelated.avsc", Record("Unrelated"));
        var consumer = Parse("consumer.avsc", Record("Consumer", "Shared", "Missing"));
        var symbols = SymbolTable.FromFiles([shared, unrelated, consumer], TestContext.Current.CancellationToken);

        var linked = LinkedAvroFile.FromInput((consumer, symbols), TestContext.Current.CancellationToken);

        Assert.Equal(
            [Name("Missing"), Name("Shared")],
            linked.References.Keys.OrderBy(static name => name.FullName, StringComparer.Ordinal));
        Assert.Null(linked.References[Name("Missing")]);
        Assert.Equal(CSharpName.FromSchemaName(Name("Shared")), linked.References[Name("Shared")]);
    }

    [Fact]
    public void Unrelated_symbol_changes_do_not_change_the_linked_file()
    {
        var shared = Parse("shared.avsc", Record("Shared"));
        var consumer = Parse("consumer.avsc", Record("Consumer", "Shared"));
        var first = LinkedAvroFile.FromInput(
            (consumer, SymbolTable.FromFiles([shared, consumer], TestContext.Current.CancellationToken)),
            TestContext.Current.CancellationToken);
        var second = LinkedAvroFile.FromInput(
            (consumer, SymbolTable.FromFiles(
                [shared, Parse("unrelated.avsc", Record("Unrelated")), consumer],
                TestContext.Current.CancellationToken)),
            TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Same_file_forward_reference_is_resolved_from_the_symbol_table()
    {
        var file = Parse(
            "schema.avsc",
            """
            {
              "type": "record",
              "name": "Root",
              "namespace": "Example",
              "fields": [
                { "name": "forward", "type": "Later" },
                {
                  "name": "definition",
                  "type": { "type": "record", "name": "Later", "fields": [] }
                }
              ]
            }
            """);
        var symbols = SymbolTable.FromFiles([file], TestContext.Current.CancellationToken);

        var linked = LinkedAvroFile.FromInput((file, symbols), TestContext.Current.CancellationToken);

        Assert.Equal(
            CSharpName.FromSchemaName(Name("Later")),
            linked.References[Name("Later")]);
    }

    [Fact]
    public void Schema_body_changes_do_not_change_the_symbol_table()
    {
        var before = Parse("shared.avsc", Record("Shared"));
        var after = Parse("shared.avsc", Record("Shared", "External"));

        var beforeSymbols = SymbolTable.FromFiles([before], TestContext.Current.CancellationToken);
        var afterSymbols = SymbolTable.FromFiles([after], TestContext.Current.CancellationToken);

        Assert.Equal(beforeSymbols, afterSymbols);
    }

    private static SchemaName Name(string name) => new(name, "Example");

    private static string Record(string name, params string[] references)
    {
        var fields = string.Join(",", references.Select((reference, index) => $$"""{"name":"field{{index}}","type":"{{reference}}"}"""));
        return $$"""{"type":"record","name":"{{name}}","namespace":"Example","fields":[{{fields}}]}""";
    }

    private static AvroFile Parse(string path, string text) =>
        AvroFile.FromInput(
            (new SourceText(path, text), new AvroParseOptions(
                TargetProfile.Modern,
                UseNullableReferenceTypes: true)),
            TestContext.Current.CancellationToken);
}
