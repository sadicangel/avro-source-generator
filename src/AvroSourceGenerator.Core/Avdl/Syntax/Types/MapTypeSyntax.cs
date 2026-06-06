namespace AvroSourceGenerator.Avdl.Syntax.Types;

public sealed record class MapTypeSyntax(
    SyntaxToken MapKeyword,
    SyntaxToken LessThanToken,
    ITypeSyntax ElementType,
    SyntaxToken GreaterThanToken)
    : ITypeSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.MapType;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return MapKeyword;
        yield return LessThanToken;
        yield return ElementType;
        yield return GreaterThanToken;
    }
}
