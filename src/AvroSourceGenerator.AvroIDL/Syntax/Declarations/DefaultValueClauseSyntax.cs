namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public sealed record class DefaultValueClauseSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken EqualsToken,
    JsonValueSyntax JsonValue)
    : SyntaxNode(SyntaxKind.DefaultValueClause, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return EqualsToken;
        yield return JsonValue;
    }
}
