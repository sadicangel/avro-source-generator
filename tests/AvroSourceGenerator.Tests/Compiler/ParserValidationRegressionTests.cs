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
    public void Invalid_enum_defaults_report_the_value_span(string value)
    {
        var source = new SourceText("test.avdl", $"schema E; enum E {{ A }} = {value};");

        var file = AvdlSchemaParser.Parse(source, Options, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(file.Diagnostics);
        var expectedSpan = source.GetSpan(source.Text.IndexOf(value, StringComparison.Ordinal), value.Length);
        Assert.Equal(AvroDiagnosticCode.InvalidSource, diagnostic.Code);
        Assert.Equal(expectedSpan, diagnostic.SourceSpan);
        Assert.Equal(
            AvroDiagnostic.InvalidSource(expectedSpan, "Enum default value must be a string.").GetMessage(),
            diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("schema R; @namespace(1) record R {}", "1", "Namespace annotation value must be a string.")]
    [InlineData("schema R; @aliases(1) record R {}", "1", "Aliases annotation value must be an array of strings.")]
    [InlineData("schema R; record R { @logicalType(1) string f; }", "1", "Logical type annotation value must be a string.")]
    [InlineData("schema R; record R { string @order(1) f; }", "1", "Order annotation value must be a string.")]
    public void Invalid_annotation_values_report_the_value_span(string text, string value, string message)
    {
        var source = new SourceText("test.avdl", text);

        var file = AvdlSchemaParser.Parse(source, Options, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(file.Diagnostics);
        var expectedSpan = source.GetSpan(source.Text.IndexOf(value, StringComparison.Ordinal), value.Length);
        Assert.Equal(AvroDiagnosticCode.InvalidSource, diagnostic.Code);
        Assert.Equal(expectedSpan, diagnostic.SourceSpan);
        Assert.Equal(AvroDiagnostic.InvalidSource(expectedSpan, message).GetMessage(), diagnostic.GetMessage());
    }

    [Fact]
    public void Internal_invalid_operation_is_not_converted_to_a_diagnostic()
    {
        var source = new SourceText("test.avdl", "schema R; record R { date value; }");
        var invalidOptions = new AvroParseOptions((GenerationTarget)int.MaxValue, true);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AvdlSchemaParser.Parse(source, invalidOptions, TestContext.Current.CancellationToken));

        Assert.Contains("Unsupported GenerationTarget", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("schema R; @x(1) @x(2) record R {}", "record")]
    [InlineData("@x(1) @x(2) protocol P {}", "protocol")]
    public void Duplicate_avdl_properties_keep_the_last_value(string text, string schemaType)
    {
        var file = AvdlSchemaParser.Parse(new SourceText("test.avdl", text), Options, TestContext.Current.CancellationToken);

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
            Options,
            TestContext.Current.CancellationToken);

        var field = Assert.Single(Assert.IsType<RecordSchema>(Assert.Single(file.Declarations)).Fields);
        Assert.Equal(2, field.Properties["x"].GetInt32());
    }

    [Theory]
    [InlineData("test.avsc", """{"type":"record","name":"R","fields":[],"x":1,"x":2}""")]
    [InlineData("test.avpr", """{"protocol":"P","types":[],"messages":{},"x":1,"x":2}""")]
    public void Duplicate_json_properties_keep_the_last_value(string path, string text)
    {
        var file = AvscSchemaParser.Parse(new SourceText(path, text), Options, TestContext.Current.CancellationToken);

        Assert.True(file.IsValid, string.Join("; ", file.Diagnostics));
        Assert.Equal(2, file.RootSchema!.Properties["x"].GetInt32());
    }

    [Fact]
    public void Duplicate_json_field_properties_keep_the_last_value()
    {
        const string text = """{"type":"record","name":"R","fields":[{"name":"f","type":"string","x":1,"x":2}]}""";
        var file = AvscSchemaParser.Parse(new SourceText("test.avsc", text), Options, TestContext.Current.CancellationToken);

        var field = Assert.Single(Assert.IsType<RecordSchema>(file.RootSchema).Fields);
        Assert.Equal(2, field.Properties["x"].GetInt32());
    }
}
