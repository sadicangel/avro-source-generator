namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonStringSyntax(SyntaxToken Literal) : JsonValueSyntax(JsonSyntaxKind.String)
{
    public string? Value
    {
        get
        {
            var text = Literal.ValueText;
            return IndexOfInvalidCharacter(text.AsSpan(), allowControlCharacters: true) < 0 ? text : null;
        }
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.JsonString;

    public override IEnumerable<ISyntaxNode> Children() { yield return Literal; }

    public static int IndexOfInvalidCharacter(ReadOnlySpan<char> text, bool allowControlCharacters)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (!allowControlCharacters && character < ' ') return index;
            if (!char.IsSurrogate(character)) continue;
            if (!char.IsHighSurrogate(character) || index + 1 == text.Length || !char.IsLowSurrogate(text[index + 1]))
                return index;
            index++;
        }
        return -1;
    }
}
