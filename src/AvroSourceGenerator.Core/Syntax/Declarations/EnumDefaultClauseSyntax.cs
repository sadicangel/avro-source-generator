namespace AvroSourceGenerator.Syntax.Declarations;

internal sealed record class EnumDefaultClauseSyntax(SyntaxToken EqualsToken, SyntaxToken Symbol) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.EnumDefaultClause;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return EqualsToken;
        yield return Symbol;
    }
}
