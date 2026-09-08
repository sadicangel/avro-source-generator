using System.Text.Json.Nodes;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class ParserArrayTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(17)]
    public void Protocol_arrays_preserve_all_items_and_order(int count)
    {
        var fields = string.Join(",", Enumerable.Range(0, count).Select(i =>
            $$"""{"name":"field{{i}}","type":["null","string"]}"""));
        var types = string.Join(",", Enumerable.Range(0, count).Select(i =>
            $$"""{"type":"error","name":"Error{{i}}","fields":[{{fields}}]}"""));
        var errors = string.Join(",", Enumerable.Range(0, count).Select(i => $"\"Error{i}\""));
        var parsed = SchemaCompilerTestHelpers.ParseJson($$"""
            {"protocol":"Service","types":[{{types}}],"messages":{
              "call":{"request":[{{fields}}],"response":"null","errors":[{{errors}}]}
            }
            }
            """);

        var protocol = Assert.IsType<ProtocolSchema>(parsed.Root);
        Assert.Equal(Enumerable.Range(0, count).Select(i => $"Error{i}"), protocol.Types.Select(type => type.SchemaName.Name));
        Assert.All(protocol.Types, type =>
            Assert.Equal(count, Assert.IsType<ErrorSchema>(type).Fields.Length));
        var message = Assert.Single(protocol.Messages);
        Assert.Equal(Enumerable.Range(0, count).Select(i => $"field{i}"), message.RequestParameters.Select(field => field.Name));
        Assert.Equal(count, message.Errors.Length);
        Assert.All(message.RequestParameters, parameter =>
            Assert.Equal(2, Assert.IsType<UnionSchema>(parameter.Type).Schemas.Length));
    }

    [Theory]
    [InlineData("types", "{}")]
    [InlineData("types", "null")]
    public void Malformed_protocol_arrays_keep_schema_diagnostics(string property, string value)
    {
        var schema = JsonNode.Parse("""{"protocol":"Service","types":[],"messages":{}}""")!;
        schema[property] = JsonNode.Parse(value);
        var exception = Assert.Throws<InvalidSchemaException>(() => SchemaCompilerTestHelpers.ParseJson(schema.ToJsonString()));
        Assert.StartsWith($"'{property}' property must be an array", exception.Message);
    }
}
