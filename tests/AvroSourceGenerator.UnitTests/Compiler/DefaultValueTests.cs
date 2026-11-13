using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Compiler;

public sealed class DefaultValueTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("42")]
    [InlineData("1.234567890123456789E+00")]
    [InlineData("1e999")]
    [InlineData("\"abc\\u00ff\"")]
    [InlineData("\"NDI=\"")]
    [InlineData("\"\\ud800\"")]
    [InlineData("[1, null, \"text\"]")]
    [InlineData("{\"unknown\":{\"value\":1.0000},\"items\":[2]}")]
    public void Defaults_remain_json_through_binding_and_serialization(string json)
    {
        // Default interpretation and validation are deliberately deferred, even for mismatched values.
        var source = new SourceText("defaults.avsc", "{\"type\":\"record\",\"name\":\"R\",\"fields\":[{\"name\":\"Value\",\"type\":\"bytes\",\"default\":" + json + "}]}");
        var file = AvroFile.Parse(source, new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12), TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        var parsed = Assert.IsType<UnboundDefaultValue>(Assert.Single(Assert.IsType<RecordSchema>(file.RootSchema).Fields).Default);
        Assert.Equal(json, parsed.Json.GetRawText());
        var bound = SchemaCompilerTestHelpers.Bind(file);
        Assert.True(bound.IsValid);
        Assert.Empty(bound.GetDiagnostics());
        Assert.Same(parsed, Assert.Single(Assert.IsType<RecordSchema>(bound.RootSchema).Fields).Default);
        using var serialized = JsonDocument.Parse(bound.RootSchema!.ToJsonString(bound.Declarations.ToDictionary(schema => schema.SchemaName)));
        Assert.Equal(json, serialized.RootElement.GetProperty("fields")[0].GetProperty("default").GetRawText());
    }

    [Fact]
    public void Raw_defaults_own_their_json_and_compare_by_content()
    {
        UnboundDefaultValue value;
        using (var document = JsonDocument.Parse("{\"number\":1.0000}"))
            value = new UnboundDefaultValue(document.RootElement);
        using var equalJson = JsonDocument.Parse("{\"number\":1.0000}");
        var equal = new UnboundDefaultValue(equalJson.RootElement);
        Assert.Equal(value, equal);
        Assert.Equal(value.GetHashCode(), equal.GetHashCode());
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) value.WriteTo(writer);
        Assert.Equal("{\"number\":1.0000}", Encoding.UTF8.GetString(stream.ToArray()));
    }
}
