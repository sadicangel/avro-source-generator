using AvroSourceGenerator.Avdl.Syntax.Annotations;
using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class MessageDeclarationSyntax(
    ITypeSyntax Type,
    SimpleNameSyntax Name,
    SyntaxList<DocumentationSyntax> Documentation,
    SyntaxList<IAnnotationSyntax> Annotations,
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
        foreach (var documentation in Documentation)
            yield return documentation;
        foreach (var annotation in Annotations)
            yield return annotation;
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
