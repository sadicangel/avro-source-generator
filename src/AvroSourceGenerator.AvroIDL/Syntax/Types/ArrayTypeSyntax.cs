
namespace AvroSourceGenerator.AvroIDL.Syntax.Types;

public sealed record class ArrayTypeSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken ArrayKeyword,
    SyntaxToken LessThanToken,
    TypeSyntax ElementType,
    SyntaxToken GreaterThanToken)
    : TypeSyntax(SyntaxKind.ArrayType, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return ArrayKeyword;
        yield return LessThanToken;
        yield return ElementType;
        yield return GreaterThanToken;
    }
}
