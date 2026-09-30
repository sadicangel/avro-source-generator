using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Compiler;

public sealed class ParserValidationRegressionTests
{
    private static readonly AvroParseOptions s_options = new(GenerationTarget.Modern, true);

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void Invalid_enum_defaults_report_the_value_span(string value)
    {
        var source = new SourceText("test.avdl", $"schema E; enum E {{ A }} = {value};");

        var file = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(file.Diagnostics);
        var expectedSpan = source.GetSourceSpan(source.Text.IndexOf(value, StringComparison.Ordinal), value.Length);
        Assert.Equal(AvroDiagnosticCode.InvalidIdlEnumDefault, diagnostic.Code);
        Assert.Equal(expectedSpan, diagnostic.SourceSpan);
        Assert.Equal(
            "Enum default must be a string or null",
            diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("schema R; @namespace(1) record R {}", "1", AvroDiagnosticCode.InvalidIdlNamespace, "Annotation '@namespace' must have a string value")]
    [InlineData("schema R; @aliases(1) record R {}", "1", AvroDiagnosticCode.InvalidIdlAliases, "Annotation '@aliases' must have an array of strings as its value")]
    [InlineData("schema R; record R { @logicalType(1) string f; }", "1", AvroDiagnosticCode.InvalidIdlLogicalTypeAnnotation, "Annotation '@logicalType' must have a string value")]
    [InlineData("schema R; @logicalType(1) fixed R(16);", "1", AvroDiagnosticCode.InvalidIdlLogicalTypeAnnotation, "Annotation '@logicalType' must have a string value")]
    [InlineData("schema R; record R { string @order(1) f; }", "1", AvroDiagnosticCode.InvalidIdlOrder, "Annotation '@order' must have a string value")]
    public void Invalid_annotation_values_report_the_value_span(string text, string value, AvroDiagnosticCode code, string message)
    {
        var source = new SourceText("test.avdl", text);

        var file = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(file.Diagnostics);
        var expectedSpan = source.GetSourceSpan(source.Text.IndexOf(value, StringComparison.Ordinal), value.Length);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(expectedSpan, diagnostic.SourceSpan);
        Assert.Equal(message, diagnostic.GetMessage());
        Assert.Empty(diagnostic.Arguments);
        Assert.Equal($"AVROSG{(int)code:D4}", diagnostic.ToDiagnostic().Id);
    }

    [Theory]
    [InlineData("schema Order.;", "Order.", "Order.")]
    [InlineData("schema com.example.Order.;", "com.example.Order.", "com.example.Order.")]
    public void Invalid_idl_references_use_the_shared_reference_diagnostic(string text, string name, string reference)
    {
        var source = new SourceText("test.avdl", text);
        var diagnostic = Assert.Single(AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken).Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidSchemaReference, diagnostic.Code);
        Assert.Equal("AVROSG2003", diagnostic.ToDiagnostic().Id);
        Assert.Equal($"Invalid type reference '{name}'; expected a name such as 'Order' or 'com.example.Order'", diagnostic.GetMessage());
        Assert.Equal(source.GetSourceSpan(text.IndexOf(reference, StringComparison.Ordinal), reference.Length), diagnostic.SourceSpan);
    }

    [Fact]
    public void Internal_invalid_operation_is_not_converted_to_a_diagnostic()
    {
        var source = new SourceText("test.avdl", "schema R; record R { date value; }");
        var invalidOptions = new AvroParseOptions((GenerationTarget)int.MaxValue, true);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AvxxParser.Parse(source, invalidOptions, TestContext.Current.CancellationToken));

        Assert.Contains("Unsupported GenerationTarget", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("schema R; @x(1) @x(2) record R {}", "record")]
    [InlineData("@x(1) @x(2) protocol P {}", "protocol")]
    public void Duplicate_avdl_properties_keep_the_last_value(string text, string schemaType)
    {
        var file = AvxxParser.Parse(new SourceText("test.avdl", text), s_options, TestContext.Current.CancellationToken);

        Assert.True(file.IsValid, string.Join("; ", file.Diagnostics));
        TopLevelSchema schema = schemaType == "protocol"
            ? Assert.IsType<ProtocolSchema>(file.RootSchema)
            : Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        Assert.Equal(2, schema.Properties["x"].GetInt32());
    }

    [Fact]
    public void Duplicate_avdl_field_properties_keep_the_last_value()
    {
        var file = AvxxParser.Parse(
            new SourceText("test.avdl", "schema R; record R { string @x(1) @x(2) value; }"),
            s_options,
            TestContext.Current.CancellationToken);

        var field = Assert.Single(Assert.IsType<RecordSchema>(Assert.Single(file.Declarations)).Fields);
        Assert.Equal(2, field.Properties["x"].GetInt32());
    }

    [Theory]
    [InlineData("test.avsc", """{"type":"record","name":"R","fields":[],"x":1,"x":2}""")]
    [InlineData("test.avpr", """{"protocol":"P","types":[],"messages":{},"x":1,"x":2}""")]
    public void Duplicate_json_properties_keep_the_last_value(string path, string text)
    {
        var file = AvxxParser.Parse(new SourceText(path, text), s_options, TestContext.Current.CancellationToken);

        Assert.True(file.IsValid, string.Join("; ", file.Diagnostics));
        Assert.Equal(2, file.RootSchema.Properties["x"].GetInt32());
    }

    [Fact]
    public void Duplicate_json_field_properties_keep_the_last_value()
    {
        const string Text = """{"type":"record","name":"R","fields":[{"name":"f","type":"string","x":1,"x":2}]}""";
        var file = AvxxParser.Parse(new SourceText("test.avsc", Text), s_options, TestContext.Current.CancellationToken);

        var field = Assert.Single(Assert.IsType<RecordSchema>(file.RootSchema).Fields);
        Assert.Equal(2, field.Properties["x"].GetInt32());
    }
}
