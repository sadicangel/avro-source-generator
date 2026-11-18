namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonBooleanSyntax(SyntaxToken Literal) : JsonValueSyntax(JsonSyntaxKind.Boolean)
{
    public bool Value => Literal.SyntaxKind == SyntaxKind.TrueKeyword;

    public override SyntaxKind SyntaxKind => SyntaxKind.JsonBoolean;

    public override IEnumerable<ISyntaxNode> Children() { yield return Literal; }
}
