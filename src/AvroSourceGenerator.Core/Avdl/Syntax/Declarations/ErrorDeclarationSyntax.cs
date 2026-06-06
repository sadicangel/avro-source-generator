namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class ErrorDeclarationSyntax(
    SyntaxToken ErrorKeyword,
    SimpleNameSyntax Name,
    SyntaxList<DocumentationSyntax> Documentation,
    SyntaxList<AnnotationSyntax> Annotations,
    SyntaxToken BraceOpenToken,
    SyntaxList<FieldDeclarationSyntax> Fields,
    SyntaxToken BraceCloseToken)
    : ISchemaDeclarationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.ErrorDeclaration;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return ErrorKeyword;
        yield return Name;
        foreach (var documentation in Documentation)
            yield return documentation;
        foreach (var annotation in Annotations)
            yield return annotation;
        yield return BraceOpenToken;
        foreach (var field in Fields)
            yield return field;
        yield return BraceCloseToken;
    }
}
