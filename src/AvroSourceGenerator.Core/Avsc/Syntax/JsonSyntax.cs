using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avsc.Syntax;

// TODO: Change abstract -> closed.
internal abstract record class JsonSyntax(SourceSpan SourceSpan, JsonTokenType TokenType)
{
    private JsonElement? _element;

    public JsonSyntax? Parent { get; init; }

    public string GetRawText() => SourceSpan.ToString();

    public JsonElement ToJsonElement()
    {
        if (_element is { } element) return element;
        using var document = JsonDocument.Parse(SourceSpan.SourceText.Text.AsMemory(SourceSpan.Offset, SourceSpan.Length));
        return (_element = document.RootElement.Clone()).Value;
    }

    public string GetDisplayText() => TokenType switch
    {
        JsonTokenType.String => ((JsonValueSyntax)this).StringValue!,
        JsonTokenType.Null => string.Empty,
        JsonTokenType.True => bool.TrueString,
        JsonTokenType.False => bool.FalseString,
        _ => GetRawText(),
    };
}
