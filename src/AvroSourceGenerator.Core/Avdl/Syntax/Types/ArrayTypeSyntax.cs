namespace AvroSourceGenerator.Avdl.Syntax.Types;

public sealed record class ArrayTypeSyntax(
    SyntaxToken ArrayKeyword,
    SyntaxToken LessThanToken,
    ITypeSyntax ItemType,
    SyntaxToken GreaterThanToken)
    : ITypeSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.ArrayType;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return ArrayKeyword;
        yield return LessThanToken;
        yield return ItemType;
        yield return GreaterThanToken;
    }
}
