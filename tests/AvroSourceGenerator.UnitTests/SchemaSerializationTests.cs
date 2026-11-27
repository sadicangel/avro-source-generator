using System.Text.Encodings.Web;
using System.Text.Json;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Serialization;

namespace AvroSourceGenerator.UnitTests;

public sealed class SchemaSerializationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Json_preserves_unicode_and_unescapes_field_names(bool indented)
    {
        // Deliberate Portuguese text exercises Unicode preservation.
        // ReSharper disable once StringLiteralTypo
        const string Source = """
            {"type":"record","name":"Example","doc":"Olá 世界 😀","fields":[
              {"name":"class","type":"string","default":"acção 😀"},
              {"name":"ordinary","type":"int"}
            ]}
            """;
        var parsed = SchemaCompilerTestHelpers.ParseJson(Source);
        var record = Assert.IsType<RecordSchema>(parsed.RootSchema);
        Assert.Equal("class", record.Fields[0].Name.SchemaName);
        Assert.Equal("@class", record.Fields[0].Name.CSharpName);

        var json = record.ToAvjs(
            options: new JsonWriterOptions
            {
                Indented = indented,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

        using var document = JsonDocument.Parse(json);
        Assert.Equal("Olá 世界 😀", document.RootElement.GetProperty("doc").GetString());
        var fields = document.RootElement.GetProperty("fields");
        Assert.Equal("class", fields[0].GetProperty("name").GetString());
        // ReSharper disable once StringLiteralTypo
        Assert.Equal("acção 😀", fields[0].GetProperty("default").GetString());
        Assert.Equal("ordinary", fields[1].GetProperty("name").GetString());
        Assert.DoesNotContain('\0', json);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Json_preserves_raw_protocol_parameter_defaults(bool indented)
    {
        const string Source = """
            {"protocol":"P","types":[],"messages":{"m":{"request":[
              {"name":"withDefault","type":{"type":"map","values":"double"},"default":{"number":1.0000}},
              {"name":"withoutDefault","type":"string"},
              {"name":"nullable","type":["null","string"],"default":null}
            ],"response":"null"}}}
            """;
        var parsed = SchemaCompilerTestHelpers.ParseJson(Source, extension: ".avpr");
        Assert.True(parsed.IsValid, string.Join(Environment.NewLine, parsed.GetDiagnostics()));

        var json = parsed.RootSchema.ToAvjs(
            options: new JsonWriterOptions { Indented = indented });

        using var document = JsonDocument.Parse(json);
        var parameters = document.RootElement.GetProperty("messages").GetProperty("m").GetProperty("request");
        Assert.Equal("""{"number":1.0000}""", parameters[0].GetProperty("default").GetRawText());
        Assert.False(parameters[1].TryGetProperty("default", out _));
        Assert.Equal(JsonValueKind.Null, parameters[2].GetProperty("default").ValueKind);
    }

    [Fact]
    public void Json_preserves_named_references_without_definitions_and_expands_available_definitions()
    {
        var order = Assert.IsType<RecordSchema>(SchemaCompilerTestHelpers.ParseJson("""
            {"type":"record","name":"Contracts.Order","fields":[{"name":"shipping","type":"Addresses.Address"}]}
            """).RootSchema);
        var address = Assert.IsType<RecordSchema>(SchemaCompilerTestHelpers.ParseJson("""
            {"type":"record","name":"Addresses.Address","fields":[{"name":"street","type":"string"}]}
            """).RootSchema);

        var json = order.ToAvjs();
        Assert.Equal(json, order.ToAvjs(new Dictionary<SchemaName, TopLevelSchema>()));
        Assert.Equal("\"Addresses.Address\"", order.Fields[0].Type.ToAvjs());
        using var withReference = JsonDocument.Parse(json);
        Assert.Equal("Addresses.Address", withReference.RootElement.GetProperty("fields")[0].GetProperty("type").GetString());

        using var withDefinition = JsonDocument.Parse(order.ToAvjs(new Dictionary<SchemaName, TopLevelSchema> { [address.SchemaName] = address }));
        var definition = withDefinition.RootElement.GetProperty("fields")[0].GetProperty("type");
        Assert.Equal("Address", definition.GetProperty("name").GetString());
        Assert.Equal("Addresses", definition.GetProperty("namespace").GetString());
    }
}
