namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonPropertySyntax(SyntaxToken Name, SyntaxToken ColonToken, JsonValueSyntax Value) : JsonSyntaxNode(JsonSyntaxKind.Property)
{
    public override SyntaxKind SyntaxKind => SyntaxKind.JsonProperty;

    public override IEnumerable<ISyntaxNode> Children()
    {
        yield return Name;
        yield return ColonToken;
        yield return Value;
    }
}
