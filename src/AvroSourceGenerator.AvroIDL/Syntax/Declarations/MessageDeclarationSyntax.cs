using AvroSourceGenerator.AvroIDL.Syntax.Names;
using AvroSourceGenerator.AvroIDL.Syntax.Types;

namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public sealed record class MessageDeclarationSyntax(
    SyntaxTree SyntaxTree,
    TypeSyntax Type,
    SimpleNameSyntax Name,
    SyntaxToken ParenthesisOpenToken,
    SyntaxList<ParameterDeclarationSyntax> Parameters,
    SyntaxToken ParenthesisCloseToken,
    OneWayClauseSyntax? OneWayClause,
    ThrowsErrorClauseSyntax? ThrowsErrorClause,
    SyntaxToken SemicolonToken)
    : SyntaxNode(SyntaxKind.MessageDeclaration, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return Type;
        yield return Name;
        yield return ParenthesisOpenToken;
        foreach (var parameter in Parameters)
        {
            yield return parameter;
        }
        yield return ParenthesisCloseToken;
        if (OneWayClause is not null)
        {
            yield return OneWayClause;
        }
        if (ThrowsErrorClause is not null)
        {
            yield return ThrowsErrorClause;
        }
        yield return SemicolonToken;
    }
}
