using System.Text.Encodings.Web;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaSerializationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Json_preserves_unicode_and_unescapes_field_names(bool indented)
    {
        const string source = """
            {"type":"record","name":"Example","doc":"Olá 世界 😀","fields":[
              {"name":"class","type":"string","default":"ação 😀"},
              {"name":"ordinary","type":"int"}
            ]}
            """;
        var parsed = SchemaCompilerTestHelpers.ParseJson(source);
        var record = Assert.IsType<RecordSchema>(parsed.Root);
        Assert.Equal("@class", record.Fields[0].Name);

        var json = record.ToJsonString(parsed.Declarations.ToDictionary(schema => schema.SchemaName),
            new JsonWriterOptions { Indented = indented, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        using var document = JsonDocument.Parse(json);
        Assert.Equal("Olá 世界 😀", document.RootElement.GetProperty("doc").GetString());
        var fields = document.RootElement.GetProperty("fields");
        Assert.Equal("class", fields[0].GetProperty("name").GetString());
        Assert.Equal("ação 😀", fields[0].GetProperty("default").GetString());
        Assert.Equal("ordinary", fields[1].GetProperty("name").GetString());
        Assert.DoesNotContain('\0', json);
    }
}
