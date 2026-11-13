using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.UnitTests;

public sealed class ParserArrayTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(17)]
    public void Protocol_arrays_preserve_all_items_and_order(int count)
    {
        var fields = string.Join(
            ",",
            Enumerable.Range(0, count).Select(i =>
                $$"""{"name":"field{{i}}","type":["null","string"]}"""));
        var types = string.Join(
            ",",
            Enumerable.Range(0, count).Select(i =>
                $$"""{"type":"error","name":"Error{{i}}","fields":[{{fields}}]}"""));
        var errors = string.Join(",", Enumerable.Range(0, count).Select(i => $"\"Error{i}\""));
        var parsed = SchemaCompilerTestHelpers.ParseJson(
            $$"""
            {"protocol":"Service","types":[{{types}}],"messages":{
              "call":{"request":[{{fields}}],"response":"null","errors":[{{errors}}]}
            }
            }
            """,
            extension: ".avpr");

        var protocol = Assert.IsType<ProtocolSchema>(parsed.RootSchema);
        Assert.Equal(Enumerable.Range(0, count).Select(i => $"Error{i}"), protocol.Types.Select(type => type.SchemaName.Name));
        Assert.All(
            protocol.Types,
            type =>
                Assert.Equal(count, Assert.IsType<ErrorSchema>(type).Fields.Length));
        var message = Assert.Single(protocol.Messages);
        Assert.Equal(Enumerable.Range(0, count).Select(i => $"field{i}"), message.RequestParameters.Select(field => field.Name.SchemaName));
        var errorUnion = Assert.IsType<UnionSchema>(message.Errors);
        Assert.Equal(count + 1, errorUnion.Schemas.Length);
        Assert.Same(AvroSchema.String, errorUnion.Schemas[0]);
        Assert.Equal(Enumerable.Range(0, count).Select(i => $"Error{i}"), errorUnion.Schemas.Skip(1).Select(error => error.SchemaName.Name));
        Assert.All(
            message.RequestParameters,
            parameter =>
                Assert.Equal(2, Assert.IsType<UnionSchema>(parameter.Type).Schemas.Length));
    }

    [Theory]
    [InlineData("types", "{}")]
    [InlineData("types", "null")]
    public void Malformed_protocol_arrays_keep_schema_diagnostics(string property, string value)
    {
        var schema = JsonNode.Parse("""{"protocol":"Service","types":[],"messages":{}}""")!;
        schema[property] = JsonNode.Parse(value);
        var diagnostic = Assert.Single(SchemaCompilerTestHelpers.ParseJson(schema.ToJsonString(), extension: ".avpr").GetDiagnostics());
        Assert.Equal(AvroDiagnosticCode.InvalidTypes, diagnostic.Code);
        Assert.Contains("must be an array of record, error, enum, or fixed definitions", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"type":"record","name":"R","fields":[{"name":false,"type":"string"},false]}""", ".avsc")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[{"name":false,"type":"string"},false],"response":"null"}}}""", ".avpr")]
    public void Arrays_report_the_first_invalid_item_before_later_shapes(string text, string extension)
    {
        var file = SchemaCompilerTestHelpers.ParseJson(text, extension: extension);

        Assert.False(file.IsValid);
        Assert.Empty(file.Declarations);
        var diagnostic = Assert.Single(file.GetDiagnostics());
        Assert.Equal("false", diagnostic.SourceSpan.ToString());
        Assert.Equal(text.IndexOf("false", StringComparison.Ordinal), diagnostic.SourceSpan.Offset);
    }
}
