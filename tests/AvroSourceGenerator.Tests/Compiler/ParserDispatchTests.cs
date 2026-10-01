using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Compiler;

public sealed class ParserDispatchTests
{
    private static readonly AvroParseOptions s_options = new AvroParseOptions(GenerationTarget.Modern, true);

    [Theory]
    [InlineData(".avsc", """{"protocol":"P"}""", AvroDiagnosticCode.SchemaExpected)]
    [InlineData(".avsc", "{}", AvroDiagnosticCode.SchemaExpected)]
    [InlineData(".avsc", "false", AvroDiagnosticCode.SchemaExpected)]
    [InlineData(".avsc", "null", AvroDiagnosticCode.SchemaExpected)]
    [InlineData(".avsc", "42", AvroDiagnosticCode.SchemaExpected)]
    [InlineData(".avpr", """{"type":"record","name":"R","fields":[]}""", AvroDiagnosticCode.ProtocolExpected)]
    [InlineData(".avpr", "{}", AvroDiagnosticCode.ProtocolExpected)]
    [InlineData(".avpr", "[]", AvroDiagnosticCode.ProtocolExpected)]
    [InlineData(".avpr", "\"string\"", AvroDiagnosticCode.ProtocolExpected)]
    [InlineData(".avpr", "false", AvroDiagnosticCode.ProtocolExpected)]
    [InlineData(".avpr", "null", AvroDiagnosticCode.ProtocolExpected)]
    [InlineData(".avpr", "42", AvroDiagnosticCode.ProtocolExpected)]
    public void Wrong_shapes_report_one_diagnostic_at_the_json_value(string extension, string json, AvroDiagnosticCode expected)
    {
        var source = new SourceText("test" + extension, " \r\n" + json + " ");
        var file = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.False(file.IsValid);
        Assert.Equal(expected, diagnostic.Code);
        Assert.Equal(source.GetSourceSpan(3, json.Length), diagnostic.SourceSpan);
        Assert.Equal(expected == AvroDiagnosticCode.SchemaExpected ? "Avro schema expected" : "Avro protocol expected", diagnostic.GetMessage());
        Assert.Empty(file.Declarations);
        Assert.Empty(file.References);
    }

    [Theory]
    [InlineData(".avsc")]
    [InlineData(".avpr")]
    public void Both_properties_follow_the_extension(string extension)
    {
        var source = new SourceText("test" + extension, """{"type":"record","name":"R","fields":[],"protocol":"P","types":[],"messages":{}}""");
        var file = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        if (extension == ".avsc") Assert.IsType<RecordSchema>(file.RootSchema);
        else Assert.IsType<ProtocolSchema>(file.RootSchema);
    }

    [Theory]
    [InlineData(".avsc", """{"type":null}""", "null", AvroDiagnosticCode.InvalidSchemaType)]
    [InlineData(".avpr", """{"protocol":false}""", "false", AvroDiagnosticCode.InvalidProtocolName)]
    public void Present_discriminators_keep_property_diagnostics(string extension, string json, string value, AvroDiagnosticCode code)
    {
        var source = new SourceText("test" + extension, json);
        var diagnostic = Assert.Single(AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken).Diagnostics);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(value, diagnostic.SourceSpan.ToString());
    }

    [Theory]
    [InlineData(".avsc", """{"type":"record","name":"R","fields":[{"name":"f","type":{"protocol":"P"}}]}""", AvroDiagnosticCode.InvalidFieldType)]
    [InlineData(".avsc", """[{"protocol":"P"}]""", AvroDiagnosticCode.SchemaExpected)]
    [InlineData(".avsc", """{"type":"array","items":{"protocol":"P"}}""", AvroDiagnosticCode.InvalidItems)]
    [InlineData(".avpr", """{"protocol":"Outer","types":[{"protocol":"P"}],"messages":{}}""", AvroDiagnosticCode.MissingSchemaProperty)]
    [InlineData(".avpr", """{"protocol":"Outer","types":[],"messages":{"m":{"request":[],"response":{"protocol":"P"}}}}""", AvroDiagnosticCode.InvalidResponse)]
    public void Nested_protocols_are_not_schemas(string extension, string json, AvroDiagnosticCode code)
    {
        var source = new SourceText("test" + extension, json);
        var diagnostic = Assert.Single(AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken).Diagnostics);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal("""{"protocol":"P"}""", diagnostic.SourceSpan.ToString());
        Assert.Same(source, diagnostic.SourceSpan.SourceText);
    }

    [Theory]
    [InlineData("test.avsc", """{"type":"record","name":"R","fields":[]}""")]
    [InlineData("test.avpr", """{"protocol":"P"}""")]
    [InlineData("test.avdl", "schema R; record R {}")]
    [InlineData("test.avsc", "{}")]
    [InlineData("test.avpr", "{")]
    [InlineData("test.avsc", " ")]
    [InlineData("test.txt", "{")]
    public void File_wrapper_preserves_dispatch_results(string path, string text)
    {
        var source = new SourceText(path, text);
        var expected = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);
        var actual = AvroFile.Parse(source, s_options, TestContext.Current.CancellationToken);
        Assert.Equal(expected, actual);
        Assert.Equal(expected.RootSchema.ToJsonString(expected.Declarations.ToDictionary(schema => schema.SchemaName)), actual.RootSchema.ToJsonString(actual.Declarations.ToDictionary(schema => schema.SchemaName)));
        Assert.Equal(expected.Diagnostics, actual.Diagnostics);
        Assert.Equal(expected.Declarations.Select(schema => schema.SchemaName), actual.Declarations.Select(schema => schema.SchemaName));
        Assert.Equal(s_options, actual.ParseOptions);
    }

    [Theory]
    [InlineData(".avsc", """{"type":"record","name":"R","fields":[{"name":"a","type":{"type":"record","name":"Nested","fields":[]}},{"name":"b","type":false}]} """, AvroDiagnosticCode.InvalidFieldType)]
    [InlineData(".avpr", """{"protocol":"P","types":[{"type":"record","name":"R","fields":[]}],"messages":{"m":{"request":[{"name":"a","type":"Missing"}],"response":false}}}""", AvroDiagnosticCode.InvalidResponse)]
    public void Invalid_roots_discard_partial_semantic_state(string extension, string json, AvroDiagnosticCode code)
    {
        var file = AvxxParser.Parse(new SourceText("test" + extension, json), s_options, TestContext.Current.CancellationToken);
        Assert.Same(AvroSchema.Null, file.RootSchema);
        Assert.Empty(file.Declarations);
        Assert.Empty(file.DeclarationSpans);
        Assert.Empty(file.References);
        Assert.Empty(file.ReferenceSpans);
        Assert.Empty(file.Dependencies);
        Assert.Equal(code, Assert.Single(file.Diagnostics).Code);
    }
}
