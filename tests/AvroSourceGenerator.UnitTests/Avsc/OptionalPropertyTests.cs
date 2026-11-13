using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Compiler.Parsing;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Avsc;

public sealed class OptionalPropertyTests
{
    [Theory]
    [InlineData("")]
    [InlineData(""","doc":null,"aliases":null,"namespace":null""")]
    public void Absent_and_nullable_properties_do_not_report_diagnostics(string optionalProperties)
    {
        var text = """{"type":"record","name":"R","fields":[]}""";
        text = text.Insert(text.Length - 1, optionalProperties);
        var file = Parse(text);
        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        Assert.Null(file.RootSchema.Documentation);
    }

    [Fact]
    public void Missing_defaults_and_explicit_null_defaults_remain_distinct()
    {
        const string Text = """{"type":"record","name":"R","fields":[{"name":"a","type":["null","string"]},{"name":"b","type":["null","string"],"default":null}]}""";
        var file = Parse(Text);
        Assert.True(file.IsValid);
        var record = Assert.IsType<RecordSchema>(file.RootSchema);
        Assert.Null(record.Fields[0].Default);
        Assert.IsType<UnboundDefaultValue>(record.Fields[1].Default);
        Assert.IsType<UnboundDefaultValue>(Assert.IsType<RecordSchema>(SchemaCompilerTestHelpers.Bind(file).RootSchema).Fields[1].Default);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("false")]
    public void Optional_message_values_preserve_valid_null_and_false(string oneWay)
    {
        var file = Parse(
            $$"""{"protocol":"P","types":[],"messages":{"m":{"request":[{"name":"p","type":"string"}],"response":"string","one-way":{{oneWay}},"errors":null,"doc":null} } }""",
            ".avpr");

        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        var message = Assert.Single(Assert.IsType<ProtocolSchema>(file.RootSchema).Messages);
        Assert.Equal(oneWay == "false" ? false : (bool?)null, message.OneWay);
        Assert.Null(message.Documentation);
        Assert.Null(message.Errors);
        var parameter = Assert.Single(message.RequestParameters);
        Assert.Null(parameter.Documentation);
        Assert.Null(parameter.Default);
    }

    [Theory]
    [InlineData("string", """{"type":"string"}""")]
    [InlineData("record", """{"type":"record","name":"R","fields":[]}""")]
    [InlineData("enum", """{"type":"enum","name":"E","symbols":["A"]}""")]
    [InlineData("fixed", """{"type":"fixed","name":"F","size":4}""")]
    public void Omitted_logical_types_preserve_apache_schema_types(string type, string text)
    {
        var file = AvxxParser.Parse(
            new SourceText("test.avsc", text),
            new AvroParseOptions(GenerationTarget.Apache, LanguageFeatures.NullableReferenceTypes),
            TestContext.Current.CancellationToken);

        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        Assert.IsNotType<LogicalSchema>(file.RootSchema);
        Assert.Equal(type, file.RootSchema.SchemaType.ToString().ToLowerInvariant());
    }

    [Theory]
    [InlineData("""{"type":"array","items":null}""")]
    [InlineData("""{"type":null}""")]
    [InlineData("""{"type":"record","name":"R","fields":[],"doc":0}""")]
    [InlineData("""{"type":"record","name":"R","fields":[],"logicalType":null}""")]
    public void Present_invalid_values_are_not_treated_as_absence(string text)
    {
        var file = Parse(text);
        Assert.False(file.IsValid);
        var diagnostic = Assert.Single(file.GetDiagnostics());
        Assert.NotEqual(AvroDiagnosticCode.None, diagnostic.Code);
        Assert.NotEmpty(diagnostic.GetMessage());
    }

    [Fact]
    public void Duplicate_messages_and_property_lookup_are_last_wins()
    {
        const string Text = """{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null"},"m":{"request":[],"response":"null"}},"custom":1,"custom":2}""";
        var file = Parse(Text, ".avpr");
        Assert.True(file.IsValid);
        var protocol = Assert.IsType<ProtocolSchema>(file.RootSchema);
        Assert.Single(protocol.Messages);
        Assert.Equal("2", protocol.Properties["custom"].GetRawText());
    }

    [Fact]
    public void Only_selected_message_values_are_semantically_validated()
    {
        const string Text = """{"protocol":"P","types":[],"messages":{"a":{"request":false,"response":"Missing"},"b":{"request":[],"response":"null"},"a":{"request":[],"response":"null"}}}""";
        var file = Parse(Text, ".avpr");
        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        Assert.Empty(file.References);
        Assert.Equal(2, Assert.IsType<ProtocolSchema>(file.RootSchema).Messages.Length);
    }

    [Theory]
    [InlineData("""{"type":"record","name":"R","fields":[1,],"fields":[]}""")]
    [InlineData("""{"type":"record","name":"R","fields":[],"unused":{"invalid":!}}""")]
    public void Discarded_or_unused_values_must_still_be_well_formed_json(string text)
    {
        var file = Parse(text);
        Assert.Equal(AvroDiagnosticCode.InvalidJson, Assert.Single(file.GetDiagnostics()).Code);
        Assert.Empty(file.Declarations);
    }

    private static AvroFile Parse(string text, string extension = ".avsc") => AvxxParser.Parse(
        new SourceText("test" + extension, text),
        new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.NullableReferenceTypes),
        TestContext.Current.CancellationToken);
}
