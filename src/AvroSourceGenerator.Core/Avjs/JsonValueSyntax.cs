using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avjs;

public sealed record class JsonValueSyntax(SourceSpan SourceSpan, JsonTokenType TokenType) : JsonSyntax(SourceSpan, TokenType)
{
    private string? _string;
    public string? GetString() => _string ??= TokenType is JsonTokenType.String ? AsJsonElement().Deserialize<string>() : null;

    public int? GetInt32() => TokenType == JsonTokenType.Number ? AsJsonElement().TryGetInt32(out var int32) ? int32 : null : null;
}
