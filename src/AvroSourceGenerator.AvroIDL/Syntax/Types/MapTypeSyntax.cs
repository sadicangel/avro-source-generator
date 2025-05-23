
namespace AvroSourceGenerator.AvroIDL.Syntax.Types;

public sealed record class MapTypeSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken MapKeyword,
    SyntaxToken LessThanToken,
    TypeSyntax ElementType,
    SyntaxToken GreaterThanToken)
    : TypeSyntax(SyntaxKind.MapType, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return MapKeyword;
        yield return LessThanToken;
        yield return ElementType;
        yield return GreaterThanToken;
    }
}
