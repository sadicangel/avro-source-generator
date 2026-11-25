using System.Text.Encodings.Web;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

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

        var json = record.ToJsonString(
            parsed.Declarations.ToDictionary(schema => schema.SchemaName),
            new JsonWriterOptions
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

        var json = parsed.RootSchema.ToJsonString(
            parsed.Declarations.ToDictionary(schema => schema.SchemaName),
            new JsonWriterOptions { Indented = indented });

        using var document = JsonDocument.Parse(json);
        var parameters = document.RootElement.GetProperty("messages").GetProperty("m").GetProperty("request");
        Assert.Equal("""{"number":1.0000}""", parameters[0].GetProperty("default").GetRawText());
        Assert.False(parameters[1].TryGetProperty("default", out _));
        Assert.Equal(JsonValueKind.Null, parameters[2].GetProperty("default").ValueKind);
    }
}
