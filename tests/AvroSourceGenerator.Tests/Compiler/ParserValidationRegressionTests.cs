using System.Text.Json.Nodes;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Compiler;

public sealed class ParserValidationRegressionTests
{
    private static readonly AvroParseOptions Options = new(GenerationTarget.Modern, true);

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void Invalid_enum_defaults_are_caught_at_the_parser_boundary(string value)
    {
        var source = new SourceText("test.avdl", $"schema E; enum E {{ A }} = {value};");
        var node = value switch
        {
            "1" => JsonValue.Create(1),
            "true" => JsonValue.Create(true),
            _ => JsonNode.Parse(value),
        };
        var expected = Assert.Throws<InvalidOperationException>(() => node!.GetValue<string>()).Message;

        var file = AvdlSchemaParser.Parse(source, Options);

        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.UnknownError, diagnostic.Code);
        Assert.Equal(source.GetSpan(0, source.Length), diagnostic.SourceSpan);
        Assert.Equal(AvroDiagnostic.UnknownError(diagnostic.SourceSpan, expected).GetMessage(), diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("schema R; @x(1) @x(2) record R {}", "record")]
    [InlineData("@x(1) @x(2) protocol P {}", "protocol")]
    public void Duplicate_avdl_properties_keep_the_last_value(string text, string schemaType)
    {
        var file = AvdlSchemaParser.Parse(new SourceText("test.avdl", text), Options);

        Assert.True(file.IsValid, string.Join("; ", file.Diagnostics));
        TopLevelSchema schema = schemaType == "protocol"
            ? Assert.IsType<ProtocolSchema>(file.RootSchema)
            : Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        Assert.Equal(2, schema.Properties["x"].GetInt32());
    }

    [Fact]
    public void Duplicate_avdl_field_properties_keep_the_last_value()
    {
        var file = AvdlSchemaParser.Parse(
            new SourceText("test.avdl", "schema R; record R { string @x(1) @x(2) value; }"),
            Options);

        var field = Assert.Single(Assert.IsType<RecordSchema>(Assert.Single(file.Declarations)).Fields);
        Assert.Equal(2, field.Properties["x"].GetInt32());
    }

    [Theory]
    [InlineData("test.avsc", """{"type":"record","name":"R","fields":[],"x":1,"x":2}""")]
    [InlineData("test.avpr", """{"protocol":"P","types":[],"messages":{},"x":1,"x":2}""")]
    public void Duplicate_json_properties_keep_the_last_value(string path, string text)
    {
        var file = AvscSchemaParser.Parse(new SourceText(path, text), Options);

        Assert.True(file.IsValid, string.Join("; ", file.Diagnostics));
        Assert.Equal(2, file.RootSchema!.Properties["x"].GetInt32());
    }

    [Fact]
    public void Duplicate_json_field_properties_keep_the_last_value()
    {
        const string text = """{"type":"record","name":"R","fields":[{"name":"f","type":"string","x":1,"x":2}]}""";
        var file = AvscSchemaParser.Parse(new SourceText("test.avsc", text), Options);

        var field = Assert.Single(Assert.IsType<RecordSchema>(file.RootSchema).Fields);
        Assert.Equal(2, field.Properties["x"].GetInt32());
    }
}
