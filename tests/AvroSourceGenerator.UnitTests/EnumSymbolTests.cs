using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.UnitTests;

public sealed class EnumSymbolTests
{
    [Theory]
    [InlineData("class")]
    [InlineData("normal")]
    public void Json_and_idl_preserve_symbols_and_defaults(string defaultSymbol)
    {
        var json = SchemaCompilerTestHelpers.ParseJson(EnumSymbolAssertions.Json(defaultSymbol));
        var idl = SchemaCompilerTestHelpers.ParseSource(EnumSymbolAssertions.Idl(defaultSymbol));
        var serialized = new List<string>();
        foreach (var file in new[] { json, idl })
        {
            Assert.True(file.IsValid);
            var record = Assert.Single(file.Declarations.OfType<RecordSchema>());
            var enumeration = Assert.Single(file.Declarations.OfType<EnumSchema>());
            Assert.Equal(["class", "normal", "int"], enumeration.Symbols.Select(symbol => symbol.SchemaName));
            Assert.Equal(["@class", "normal", "@int"], enumeration.Symbols.Select(symbol => symbol.CSharpName));
            var text = record.ToJsonString(file.Declarations.ToDictionary(schema => schema.SchemaName));
            serialized.Add(text);
            using var document = JsonDocument.Parse(text);
            var field = document.RootElement.GetProperty("fields")[0];
            Assert.Equal(defaultSymbol, field.GetProperty("default").GetString());
            var type = field.GetProperty("type");
            Assert.Equal(["class", "normal", "int"], type.GetProperty("symbols").EnumerateArray().Select(symbol => symbol.GetString()));
            Assert.Equal(defaultSymbol, type.GetProperty("default").GetString());
        }
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(serialized[0]), JsonNode.Parse(serialized[1])));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("normal")]
    public void Standalone_json_and_idl_compile_with_keyword_members(string defaultSymbol) =>
        EnumSymbolAssertions.Verify("None", [], defaultSymbol);
}
