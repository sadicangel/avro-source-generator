using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.UnitTests;

public sealed class ProtocolErrorTests
{
    [Theory]
    [InlineData("")]
    [InlineData(""","errors":null""")]
    public void Omitted_errors_use_the_implicit_system_error(string errors)
    {
        var file = SchemaCompilerTestHelpers.ParseJson(
            $$"""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null"{{errors}} } } }""",
            extension: ".avpr");

        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        var protocol = Assert.IsType<ProtocolSchema>(file.RootSchema);
        var message = Assert.Single(protocol.Messages);
        Assert.Null(message.Errors);
        Assert.Null(message.OneWay);
        using var json = JsonDocument.Parse(protocol.ToJsonString(file.Declarations.ToDictionary(schema => schema.SchemaName)));
        Assert.False(json.RootElement.GetProperty("messages").GetProperty("m").TryGetProperty("errors", out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Declared_errors_prepend_string_and_serialize_only_the_declarations(bool idl)
    {
        var file = idl
            ? SchemaCompilerTestHelpers.ParseSource("protocol P { error A {} error B {} void m() throws B, A; }")
            : SchemaCompilerTestHelpers.ParseJson(
                """{"protocol":"P","types":[{"type":"error","name":"A","fields":[]},{"type":"error","name":"B","fields":[]}],"messages":{"m":{"request":[],"response":"null","errors":["B","A"]}}}""",
                extension: ".avpr");

        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        var protocol = Assert.IsType<ProtocolSchema>(file.RootSchema);
        var union = Assert.IsType<UnionSchema>(Assert.Single(protocol.Messages).Errors);
        Assert.Equal(["string", "B", "A"], union.Schemas.Select(schema => schema.SchemaName.FullName));
        Assert.Same(AvroSchema.String, union.Schemas[0]);
        var bound = Assert.IsType<ProtocolSchema>(SchemaCompilerTestHelpers.Bind(file).RootSchema);
        Assert.Equal(union.Schemas, Assert.IsType<UnionSchema>(Assert.Single(bound.Messages).Errors).Schemas);
        using var json = JsonDocument.Parse(protocol.ToJsonString(file.Declarations.ToDictionary(schema => schema.SchemaName)));
        Assert.Equal(["B", "A"], json.RootElement.GetProperty("messages").GetProperty("m").GetProperty("errors").EnumerateArray().Select(error => error.GetString()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(""","errors":null""")]
    [InlineData(""","errors":[]""")]
    public void One_way_messages_allow_only_the_implicit_system_error(string errors)
    {
        var file = SchemaCompilerTestHelpers.ParseJson(
            $$"""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null","one-way":true{{errors}} } } }""",
            extension: ".avpr");

        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        var protocol = Assert.IsType<ProtocolSchema>(file.RootSchema);
        var message = Assert.Single(protocol.Messages);
        Assert.True(message.OneWay);
        if (errors.Contains('['))
            Assert.Same(AvroSchema.String, Assert.Single(Assert.IsType<UnionSchema>(message.Errors).Schemas));
        else
            Assert.Null(message.Errors);
        using var json = JsonDocument.Parse(protocol.ToJsonString(file.Declarations.ToDictionary(schema => schema.SchemaName)));
        Assert.False(json.RootElement.GetProperty("messages").GetProperty("m").TryGetProperty("errors", out _));
    }

    [Theory]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"string","one-way":true}}}""", false)]
    [InlineData("""{"protocol":"P","types":[{"type":"error","name":"E","fields":[]}],"messages":{"m":{"request":[],"response":"null","errors":["E"],"one-way":true}}}""", false)]
    [InlineData("protocol P { string m() oneway; }", true)]
    [InlineData("protocol P { error E {} void m() oneway throws E; }", true)]
    public void One_way_messages_reject_responses_and_declared_errors(string text, bool idl)
    {
        var file = idl
            ? SchemaCompilerTestHelpers.ParseSource(text)
            : SchemaCompilerTestHelpers.ParseJson(text, extension: ".avpr");

        Assert.False(file.IsValid);
        Assert.Equal(idl ? AvroDiagnosticCode.InvalidIdlOneWayMessage : AvroDiagnosticCode.InvalidOneWayMessage, Assert.Single(file.GetDiagnostics()).Code);
    }
}
