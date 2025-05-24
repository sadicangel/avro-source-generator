
namespace AvroSourceGenerator.AvroIDL.Syntax.Types;
public sealed record class UnionTypeSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken UnionKeyword,
    SyntaxToken BraceOpenToken,
    SyntaxList<TypeSyntax> Types,
    SyntaxToken BraceCloseToken)
    : TypeSyntax(SyntaxKind.UnionType, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return UnionKeyword;
        yield return BraceOpenToken;
        foreach (var type in Types)
            yield return type;
        yield return BraceCloseToken;
    }
}
