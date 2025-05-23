using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public sealed record class FixedDeclarationSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken FixedKeyword,
    SimpleNameSyntax Name,
    SyntaxToken ParenthesisOpenToken,
    SyntaxToken SizeLiteralToken,
    SyntaxToken ParenthesisCloseToken)
    : SchemaDeclarationSyntax(SyntaxKind.FixedDeclaration, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return FixedKeyword;
        yield return Name;
        yield return ParenthesisOpenToken;
        yield return SizeLiteralToken;
        yield return ParenthesisCloseToken;
    }
}
