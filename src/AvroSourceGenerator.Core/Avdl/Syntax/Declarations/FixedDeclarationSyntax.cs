using AvroSourceGenerator.Avdl.Syntax.Annotations;

namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class FixedDeclarationSyntax(
    SyntaxToken FixedKeyword,
    SimpleNameSyntax Name,
    SyntaxList<DocumentationSyntax> Documentation,
    SyntaxList<IAnnotationSyntax> Annotations,
    SyntaxToken ParenthesisOpenToken,
    SyntaxToken SizeLiteralToken,
    SyntaxToken ParenthesisCloseToken,
    SyntaxToken SemicolonToken)
    : ISchemaDeclarationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.FixedDeclaration;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return FixedKeyword;
        yield return Name;
        foreach (var documentation in Documentation)
            yield return documentation;
        foreach (var annotation in Annotations)
            yield return annotation;
        yield return ParenthesisOpenToken;
        yield return SizeLiteralToken;
        yield return ParenthesisCloseToken;
        yield return SemicolonToken;
    }
}
