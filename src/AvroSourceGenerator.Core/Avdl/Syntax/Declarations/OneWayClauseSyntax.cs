namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class OneWayClauseSyntax(SyntaxToken OneWayKeyword) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.OneWayClause;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return OneWayKeyword;
    }
}
