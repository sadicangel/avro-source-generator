using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class FieldDeclarationSyntax(
    ITypeSyntax Type,
    SimpleNameSyntax Name,
    SyntaxList<DocumentationSyntax> Documentation,
    SyntaxList<AnnotationSyntax> Annotations,
    DefaultValueClauseSyntax? DefaultValueClause,
    SyntaxToken SemicolonToken)
    : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.FieldDeclaration;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return Type;
        yield return Name;
        foreach (var documentation in Documentation)
            yield return documentation;
        foreach (var annotation in Annotations)
            yield return annotation;
        if (DefaultValueClause is not null)
            yield return DefaultValueClause;
        yield return SemicolonToken;
    }
}
