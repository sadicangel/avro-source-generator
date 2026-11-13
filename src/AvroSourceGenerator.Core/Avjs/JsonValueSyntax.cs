using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avjs;

internal sealed record class JsonValueSyntax(SourceSpan SourceSpan, JsonTokenType TokenType) : JsonSyntax(SourceSpan, TokenType)
{
    public string? StringValue => field ??= TokenType is JsonTokenType.String ? AsJsonElement().Deserialize<string>() : null;

    public int? Int32Value => TokenType == JsonTokenType.Number ? AsJsonElement().TryGetInt32(out var int32) ? int32 : null : null;
}
