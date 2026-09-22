using System.Text.Json;
using AvroSourceGenerator.Avsc.Syntax;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Avsc;

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
        Assert.Empty(file.Diagnostics);
        Assert.Null(file.RootSchema.Documentation);
    }

    [Fact]
    public void Missing_defaults_and_explicit_null_defaults_remain_distinct()
    {
        const string text = """{"type":"record","name":"R","fields":[{"name":"a","type":["null","string"]},{"name":"b","type":["null","string"],"default":null}]}""";
        var file = Parse(text);
        Assert.True(file.IsValid);
        var record = Assert.IsType<RecordSchema>(file.RootSchema);
        Assert.Null(record.Fields[0].DefaultJson);
        Assert.Equal(JsonValueKind.Null, record.Fields[1].DefaultJson!.Value.ValueKind);
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
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.NotEqual(AvroDiagnosticCode.None, diagnostic.Code);
        Assert.NotEmpty(diagnostic.GetMessage());
    }

    [Fact]
    public void Duplicate_messages_and_property_lookup_are_last_wins()
    {
        const string text = """{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null"},"m":{"request":[],"response":"null"}},"custom":1,"custom":2}""";
        var file = Parse(text);
        Assert.True(file.IsValid);
        var protocol = Assert.IsType<ProtocolSchema>(file.RootSchema);
        Assert.Single(protocol.Messages);
        Assert.Equal("2", protocol.Properties["custom"].GetRawText());
    }

    private static AvroFile Parse(string text) => AvscParser.Parse(new SourceText("test.avsc", text),
        new AvroParseOptions(GenerationTarget.Modern, true), TestContext.Current.CancellationToken);

    [Fact]
    public void Only_selected_message_values_are_semantically_validated()
    {
        const string text = """{"protocol":"P","types":[],"messages":{"a":{"request":false,"response":"Missing"},"b":{"request":[],"response":"null"},"a":{"request":[],"response":"null"}}}""";
        var file = Parse(text);
        Assert.True(file.IsValid);
        Assert.Empty(file.Diagnostics);
        Assert.Empty(file.References);
        Assert.Equal(2, Assert.IsType<ProtocolSchema>(file.RootSchema).Messages.Length);
    }

    [Theory]
    [InlineData("""{"type":"record","name":"R","fields":[1,],"fields":[]}""")]
    [InlineData("""{"type":"record","name":"R","fields":[],"unused":{"invalid":!}}""")]
    public void Discarded_or_unused_values_must_still_be_well_formed_json(string text)
    {
        var file = Parse(text);
        Assert.Equal(AvroDiagnosticCode.InvalidJson, Assert.Single(file.Diagnostics).Code);
        Assert.Empty(file.Declarations);
    }
}
