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
    [InlineData("9223372036854775808")]
    [InlineData("-9223372036854775809")]
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
        Assert.Equal(json, parsed.JsonText);
        var bound = SchemaCompilerTestHelpers.Bind(file);
        Assert.True(bound.IsValid);
        Assert.Empty(bound.GetDiagnostics());
        Assert.Same(parsed, Assert.Single(Assert.IsType<RecordSchema>(bound.RootSchema).Fields).Default);
        using var serialized = JsonDocument.Parse(bound.RootSchema!.ToJsonString(bound.Declarations.ToDictionary(schema => schema.SchemaName)));
        Assert.Equal(json, serialized.RootElement.GetProperty("fields")[0].GetProperty("default").GetRawText());
    }

    [Fact]
    public void Raw_defaults_compare_by_content_without_normalizing_json()
    {
        const string Json = """{"number":1.0000}""";
        DefaultValue value = new UnboundDefaultValue(Json);
        DefaultValue equal = new UnboundDefaultValue(new string(Json.AsSpan()));

        Assert.Equal(value, equal);
        Assert.Equal(value.GetHashCode(), equal.GetHashCode());
        Assert.NotEqual(value, new UnboundDefaultValue("""{"number":1.0}"""));
    }

    [Theory]
    [InlineData("null", SchemaType.Null)]
    [InlineData("\"null\"", SchemaType.String)]
    public void Optional_idl_types_order_branches_using_the_default_literal(string json, SchemaType firstBranch)
    {
        var file = SchemaCompilerTestHelpers.ParseSource("schema R; record R { string? Value = /* default */ " + json + "; }");
        Assert.True(file.IsValid, string.Join(Environment.NewLine, file.GetDiagnostics()));
        var record = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        var field = Assert.Single(record.Fields);
        Assert.Equal(json, Assert.IsType<UnboundDefaultValue>(field.Default).JsonText);
        Assert.Equal(firstBranch, Assert.IsType<UnionSchema>(field.Type).Schemas[0].SchemaType);
    }
}
