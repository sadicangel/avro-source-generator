using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class MessageDeclarationSyntax(
    ITypeSyntax Type,
    SimpleNameSyntax Name,
    SyntaxToken ParenthesisOpenToken,
    SeparatedSyntaxList<ParameterDeclarationSyntax> Parameters,
    SyntaxToken ParenthesisCloseToken,
    OneWayClauseSyntax? OneWayClause,
    ThrowsErrorClauseSyntax? ThrowsErrorClause,
    SyntaxToken SemicolonToken)
    : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.MessageDeclaration;

    public IEnumerable<ISyntaxNode> Children()
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
