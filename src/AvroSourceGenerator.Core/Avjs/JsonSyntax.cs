using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avjs;

// TODO: Change abstract -> closed.
public abstract record class JsonSyntax(SourceSpan SourceSpan, JsonTokenType TokenType)
{
    private JsonElement? _element;

    public JsonSyntax? Parent { get; init; }

    public string GetRawText() => SourceSpan.ToString();

    public JsonElement AsJsonElement() => _element ??= JsonSerializer.Deserialize<JsonElement>(SourceSpan.AsSpan());

    public string GetDisplayText() => TokenType switch
    {
        JsonTokenType.String => ((JsonValueSyntax)this).GetString()!,
        JsonTokenType.Null => string.Empty,
        JsonTokenType.True => bool.TrueString,
        JsonTokenType.False => bool.FalseString,
        _ => GetRawText(),
    };
}
