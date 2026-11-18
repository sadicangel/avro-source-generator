namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonNullSyntax(SyntaxToken Literal) : JsonValueSyntax(JsonSyntaxKind.Null)
{
    public override SyntaxKind SyntaxKind => SyntaxKind.JsonNull;

    public override IEnumerable<ISyntaxNode> Children() { yield return Literal; }
}
