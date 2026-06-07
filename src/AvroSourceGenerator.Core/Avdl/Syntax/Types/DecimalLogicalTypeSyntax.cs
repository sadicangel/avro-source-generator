namespace AvroSourceGenerator.Avdl.Syntax.Types;

public sealed record class DecimalLogicalTypeSyntax(
    SyntaxToken DecimalKeyword,
    SyntaxToken ParenthesisOpenToken,
    SyntaxToken PrecisionLiteralToken,
    SyntaxToken CommaToken,
    SyntaxToken ScaleLiteralToken,
    SyntaxToken ParenthesisCloseToken)
    : ILogicalTypeSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.LogicalType;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return DecimalKeyword;
        yield return ParenthesisOpenToken;
        yield return PrecisionLiteralToken;
        yield return CommaToken;
        yield return ScaleLiteralToken;
        yield return ParenthesisCloseToken;
    }
}
