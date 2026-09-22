using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avsc.Syntax;

internal sealed record class JsonValueSyntax(SourceSpan SourceSpan, JsonTokenType TokenType) : JsonSyntax(SourceSpan, TokenType)
{
    public string? AsString() => TokenType is JsonTokenType.String or JsonTokenType.Null ? StringValue : null;

    private string? _stringValue;

    public string? StringValue
    {
        get
        {
            return TokenType == JsonTokenType.String
                ? _stringValue ??= JsonSerializer.Deserialize<string>(SourceSpan.AsSpan())
                : null;
        }
    }

    public int? Int32Value => TokenType == JsonTokenType.Number && ToJsonElement().TryGetInt32(out var value) ? value : null;
}
