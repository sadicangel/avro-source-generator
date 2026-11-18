namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonPropertySyntax(SyntaxToken Name, SyntaxToken ColonToken, JsonValueSyntax Value) : IJsonSyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.JsonProperty;

    public JsonSyntaxKind JsonSyntaxKind => JsonSyntaxKind.Property;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return Name;
        yield return ColonToken;
        yield return Value;
    }
}
