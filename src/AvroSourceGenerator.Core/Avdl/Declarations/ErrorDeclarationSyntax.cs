using AvroSourceGenerator.Avdl.Annotations;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Declarations;

namespace AvroSourceGenerator.Avdl.Declarations;

public sealed record class ErrorDeclarationSyntax(
    SyntaxToken ErrorKeyword,
    SimpleNameSyntax Name,
    SyntaxList<DocumentationSyntax> Documentation,
    SyntaxList<IAnnotationSyntax> Annotations,
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
