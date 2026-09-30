using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Avsc;

public sealed class PropertyDiagnosticTests
{
    [Fact]
    public void Schema_name_default_is_distinct_from_constructed_names()
    {
        Assert.True(default(SchemaName).IsDefault);
        Assert.False(new SchemaName("R").IsDefault);
        Assert.False(new SchemaName(string.Empty).IsDefault);
    }

    [Theory]
    [InlineData("""{"type":"record","name":false,"fields":[]}""", ".avsc", AvroDiagnosticCode.InvalidSchemaName, "false", "Property 'name' must be a non-empty string containing a valid Avro name, optionally prefixed with its namespace")]
    [InlineData("""{"protocol":false,"types":[],"messages":{}}""", ".avpr", AvroDiagnosticCode.InvalidProtocolName, "false", "Property 'protocol' must be a non-empty string containing a valid Avro name, optionally prefixed with its namespace")]
    [InlineData("""{"type":"record","name":"R","fields":[{"name":false,"type":"null"}]}""", ".avsc", AvroDiagnosticCode.InvalidFieldName, "false", "Field property 'name' must be a non-empty string containing a valid Avro name")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[{"name":false,"type":"null"}],"response":"null"}}}""", ".avpr", AvroDiagnosticCode.InvalidRequestParameterName, "false", "Request parameter property 'name' must be a non-empty string containing a valid Avro name")]
    [InlineData("""{"type":"record","name":"R","namespace":"bad..ns","fields":[]}""", ".avsc", AvroDiagnosticCode.InvalidNamespace, "\"bad..ns\"", "Property 'namespace' must be a string containing a valid Avro namespace, or null")]
    [InlineData("""{"type":false}""", ".avsc", AvroDiagnosticCode.InvalidSchemaType, "false", "Property 'type' must be a non-empty string naming an Avro type or referencing a named type")]
    [InlineData("""{"type":"record","name":"R","fields":[{"name":"f","type":false}]}""", ".avsc", AvroDiagnosticCode.InvalidFieldType, "false", "Field property 'type' must be a primitive type name, a named type reference, an inline type definition, or a union")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[{"name":"p","type":false}],"response":"null"}}}""", ".avpr", AvroDiagnosticCode.InvalidParameterType, "false", "Request parameter property 'type' must be a primitive type name, a named type reference, an inline type definition, or a union")]
    [InlineData("""{"type":"record","name":"R","fields":false}""", ".avsc", AvroDiagnosticCode.InvalidFields, "false", "Property 'fields' must be an array of field objects")]
    [InlineData("""{"type":"array","items":false}""", ".avsc", AvroDiagnosticCode.InvalidItems, "false", "Property 'items' must be a primitive type name, a named type reference, an inline type definition, or a union")]
    [InlineData("""{"type":"map","values":false}""", ".avsc", AvroDiagnosticCode.InvalidValues, "false", "Property 'values' must be a primitive type name, a named type reference, an inline type definition, or a union")]
    [InlineData("""{"type":"enum","name":"E","symbols":[false]}""", ".avsc", AvroDiagnosticCode.InvalidSymbols, "false", "Property 'symbols' must be an array of strings containing valid Avro names")]
    [InlineData("""{"type":"record","name":"R","fields":[],"aliases":[false]}""", ".avsc", AvroDiagnosticCode.InvalidAliases, "false", "Property 'aliases' must be an array of non-empty strings, or null")]
    [InlineData("""{"type":"record","name":"R","fields":[],"doc":false}""", ".avsc", AvroDiagnosticCode.InvalidDoc, "false", "Property 'doc' must be a string or null")]
    [InlineData("""{"type":"record","name":"R","fields":[],"logicalType":false}""", ".avsc", AvroDiagnosticCode.InvalidLogicalType, "false", "Property 'logicalType' must be a non-empty string")]
    [InlineData("""{"protocol":"P","types":false,"messages":{}}""", ".avpr", AvroDiagnosticCode.InvalidTypes, "false", "Property 'types' must be an array of record, error, enum, or fixed definitions")]
    [InlineData("""{"protocol":"P","types":[],"messages":false}""", ".avpr", AvroDiagnosticCode.InvalidMessages, "false", "Property 'messages' must be an object whose values are message objects")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":false,"response":"null"}}}""", ".avpr", AvroDiagnosticCode.InvalidRequest, "false", "Property 'request' must be an array of request parameter objects")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":false}}}""", ".avpr", AvroDiagnosticCode.InvalidResponse, "false", "Property 'response' must be a primitive type name, a named type reference, an inline type definition, or a union")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null","errors":[false]}}}""", ".avpr", AvroDiagnosticCode.InvalidErrors, "false", "Property 'errors' must be null or an array of primitive type names, named type references, inline type definitions, or unions")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null","one-way":0}}}""", ".avpr", AvroDiagnosticCode.InvalidOneWay, "0", "Property 'one-way' must be a boolean or null")]
    [InlineData("""{"type":"enum","name":"E","symbols":["A"],"default":false}""", ".avsc", AvroDiagnosticCode.InvalidEnumDefault, "false", "Enum property 'default' must be a string or null")]
    [InlineData("""{"protocol":"P","types":[{"type":"unknown"}],"messages":{}}""", ".avpr", AvroDiagnosticCode.InvalidProtocolDeclarationType, "\"unknown\"", "Protocol type declaration property 'type' must be 'record', 'error', 'enum', or 'fixed'")]
    [InlineData("\"bad-name\"", ".avsc", AvroDiagnosticCode.InvalidSchemaReference, "\"bad-name\"", "Invalid type reference 'bad-name'; expected a name such as 'Order' or 'com.example.Order'")]
    [InlineData("""{"type":"fixed","name":"F","size":0}""", ".avsc", AvroDiagnosticCode.InvalidFixedSize, "0", "Property 'size' must be a positive 32-bit integer")]
    public void Invalid_values_have_property_specific_codes_messages_and_value_spans(
        string json, string extension, AvroDiagnosticCode code, string value, string message)
    {
        var source = new SourceText("test" + extension, json);
        var diagnostic = Assert.Single(AvxxParser.Parse(source, Options, TestContext.Current.CancellationToken).Diagnostics);

        Assert.Equal(code, diagnostic.Code);
        Assert.Equal($"AVROSG{(int)code:D4}", diagnostic.ToDiagnostic().Id);
        Assert.Equal(message, diagnostic.GetMessage());
        Assert.Equal(source.GetSourceSpan(json.IndexOf(value, StringComparison.Ordinal), value.Length), diagnostic.SourceSpan);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"bad-name\"")]
    [InlineData("false")]
    [InlineData("\"bad..Name\"")]
    public void Schema_name_failure_modes_share_one_diagnostic(string value)
    {
        var json = "{\"type\":\"record\",\"name\":" + value + ",\"fields\":[]}";
        var diagnostic = Assert.Single(Parse(json, ".avsc").Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidSchemaName, diagnostic.Code);
        Assert.Equal(value, diagnostic.SourceSpan.ToString());
        Assert.Equal("Property 'name' must be a non-empty string containing a valid Avro name, optionally prefixed with its namespace", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("""{"type":"record","name":"R","fields":[false]}""", ".avsc", AvroDiagnosticCode.InvalidFields)]
    [InlineData("""{"protocol":"P","types":[false],"messages":{}}""", ".avpr", AvroDiagnosticCode.InvalidTypes)]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":false}}""", ".avpr", AvroDiagnosticCode.InvalidMessages)]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[false],"response":"null"}}}""", ".avpr", AvroDiagnosticCode.InvalidRequest)]
    public void Direct_invalid_elements_use_the_containing_property_code(string json, string extension, AvroDiagnosticCode code)
    {
        var diagnostic = Assert.Single(Parse(json, extension).Diagnostics);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal("false", diagnostic.SourceSpan.ToString());
    }

    [Fact]
    public void Nested_property_errors_keep_their_own_code()
    {
        const string Json = """{"type":"array","items":{"type":"record","name":false,"fields":[]}}""";
        var diagnostic = Assert.Single(Parse(Json, ".avsc").Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidSchemaName, diagnostic.Code);
        Assert.Equal("false", diagnostic.SourceSpan.ToString());
    }

    [Theory]
    [InlineData("[[\"bad-name\"]]", ".avsc", AvroDiagnosticCode.InvalidSchemaReference, "\"bad-name\"")]
    [InlineData("[[false]]", ".avsc", AvroDiagnosticCode.SchemaExpected, "false")]
    [InlineData("""{"type":"array","items":[[false]]}""", ".avsc", AvroDiagnosticCode.InvalidItems, "false")]
    [InlineData("""{"type":"map","values":[[false]]}""", ".avsc", AvroDiagnosticCode.InvalidValues, "false")]
    [InlineData("""{"type":"record","name":"R","fields":[{"name":"f","type":[["bad-name"]]}]}""", ".avsc", AvroDiagnosticCode.InvalidFieldType, "\"bad-name\"")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[{"name":"p","type":[["bad-name"]]}],"response":"null"}}}""", ".avpr", AvroDiagnosticCode.InvalidParameterType, "\"bad-name\"")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":[["bad-name"]]}}}""", ".avpr", AvroDiagnosticCode.InvalidResponse, "\"bad-name\"")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null","errors":[["bad-name"]]}}}""", ".avpr", AvroDiagnosticCode.InvalidErrors, "\"bad-name\"")]
    [InlineData("""{"type":"record","name":"R","fields":[{"name":"f","type":["null",{"type":false}]}]}""", ".avsc", AvroDiagnosticCode.InvalidSchemaType, "false")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":[{"type":"record","name":false,"fields":[]}]}}}""", ".avpr", AvroDiagnosticCode.InvalidSchemaName, "false")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"type":false}}""", ".avpr", AvroDiagnosticCode.InvalidMessages, "false")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"name":false}}""", ".avpr", AvroDiagnosticCode.InvalidMessages, "false")]
    public void Union_elements_and_nested_definitions_use_their_owning_property(
        string json, string extension, AvroDiagnosticCode code, string value)
    {
        var diagnostic = Assert.Single(Parse(json, extension).Diagnostics);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(value, diagnostic.SourceSpan.ToString());
    }

    [Fact]
    public void Missing_required_property_remains_separate()
    {
        var diagnostic = Assert.Single(Parse("""{"type":"record","fields":[]}""", ".avsc").Diagnostics);
        Assert.Equal(AvroDiagnosticCode.MissingSchemaProperty, diagnostic.Code);
        Assert.Equal("Required property 'name' is missing", diagnostic.GetMessage());
    }

    private static AvroFile Parse(string json, string extension) => AvxxParser.Parse(
        new SourceText("test" + extension, json), Options, TestContext.Current.CancellationToken);

    private static AvroParseOptions Options { get; } = new(GenerationTarget.Modern, true);
}
