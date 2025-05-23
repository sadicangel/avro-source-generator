namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public sealed record class OneWayClauseSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken OneWayKeyword)
    : SyntaxNode(SyntaxKind.OneWayClause, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return OneWayKeyword;
    }
}
