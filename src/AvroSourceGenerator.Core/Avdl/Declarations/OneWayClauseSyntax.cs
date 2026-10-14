namespace AvroSourceGenerator.Avdl.Declarations;

internal sealed record class OneWayClauseSyntax(SyntaxToken OneWayKeyword) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.OneWayClause;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return OneWayKeyword;
    }
}
