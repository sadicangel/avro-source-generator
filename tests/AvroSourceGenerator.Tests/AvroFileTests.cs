using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests;

public sealed class AvroFileTests
{
    [Fact]
    public void Invalid_file_equality_includes_content()
    {
        var file = Parse("schema.avsc", "not json");
        var sameFile = Parse("schema.avsc", "not json");
        var changedFile = Parse("schema.avsc", "still not json");

        Assert.Equal(file, sameFile);
        Assert.NotEqual(file, changedFile);
    }

    [Fact]
    public void References_include_only_external_named_uses()
    {
        var file = Parse(
            "schema.avsc",
            """
            {
              "type": "record",
              "name": "Root",
              "namespace": "Example",
              "fields": [
                {
                  "name": "definition",
                  "type": {
                    "type": "record",
                    "name": "Local",
                    "fields": []
                  }
                },
                { "name": "local", "type": "Local" },
                { "name": "external", "type": "External" }
              ]
            }
            """);

        Assert.True(file.IsValid);
        Assert.Equal([new SchemaName("External", "Example")], file.References);
    }

    [Fact]
    public void Same_file_forward_reference_remains_external()
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
                  "type": {
                    "type": "record",
                    "name": "Later",
                    "fields": []
                  }
                }
              ]
            }
            """);

        Assert.True(file.IsValid);
        Assert.Equal([new SchemaName("Later", "Example")], file.References);
    }

    [Fact]
    public void Self_reference_is_internal()
    {
        var file = Parse(
            "schema.avsc",
            """
            {
              "type": "record",
              "name": "Node",
              "namespace": "Example",
              "fields": [
                { "name": "next", "type": ["null", "Node"], "default": null }
              ]
            }
            """);

        Assert.True(file.IsValid);
        Assert.Empty(file.References);
    }

    [Fact]
    public void Internal_fixed_reference_uses_declared_csharp_name()
    {
        var file = Parse(
            "protocol.avpr",
            """
            {
              "protocol": "Api",
              "namespace": "Example",
              "types": [
                { "type": "fixed", "name": "Hash", "size": 16 },
                {
                  "type": "record",
                  "name": "Message",
                  "fields": [
                    { "name": "hash", "type": "Hash" }
                  ]
                }
              ],
              "messages": {}
            }
            """);

        Assert.True(file.IsValid);
        Assert.Empty(file.References);
        var message = Assert.IsType<RecordSchema>(file.Declarations.Single(schema => schema.SchemaName.Name == "Message"));
        var reference = Assert.IsType<AvroSchemaReference>(Assert.Single(message.Fields).Type);
        Assert.Equal(AvroSchema.Bytes.CSharpName, reference.CSharpName);
    }

    [Fact]
    public void Symbolic_root_is_a_reference()
    {
        var file = Parse("schema.avsc", "\"Example.External\"");

        Assert.True(file.IsValid);
        Assert.Equal([new SchemaName("External", "Example")], file.References);
        Assert.Empty(file.Declarations);
    }

    [Fact]
    public void Avdl_root_directive_resolves_after_declarations()
    {
        var file = Parse(
            "schema.avdl",
            """
            namespace Example;
            schema Message;

            record Message {
                string value;
            }
            """);

        Assert.True(file.IsValid);
        Assert.Empty(file.References);
        Assert.Equal(new SchemaName("Message", "Example"), file.RootSchema!.SchemaName);
    }

    [Fact]
    public void Avdl_imports_are_exposed_as_paths()
    {
        var file = Parse(
            "schema.avdl",
            """
            import idl "common.avdl";
            schema string;
            """);

        Assert.True(file.IsValid);
        Assert.Equal(["common.avdl"], file.Imports);
    }

    [Fact]
    public void Avdl_syntax_diagnostics_are_preserved()
    {
        const string text = "$ record User { # string name; }";

        var file = Parse("schema.avdl", text);

        Assert.False(file.IsValid);
        Assert.Contains(file.Diagnostics, diagnostic => diagnostic.Location.TextSpan.Start == text.IndexOf('$'));
        Assert.Contains(file.Diagnostics, diagnostic => diagnostic.Location.TextSpan.Start == text.IndexOf('#'));
    }

    private static AvroFile Parse(string path, string text) =>
        AvroFile.FromInput(
            (new SourceText(path, text), new AvroParseOptions(
                TargetProfile.Modern,
                UseNullableReferenceTypes: true)),
            TestContext.Current.CancellationToken);
}
