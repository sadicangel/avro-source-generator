namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class ProtocolDeclarationSyntax(
    SyntaxToken ProtocolKeyword,
    SimpleNameSyntax Name,
    SyntaxList<DocumentationSyntax> Documentation,
    SyntaxList<AnnotationSyntax> Annotations,
    SyntaxToken BraceOpenToken,
    SyntaxList<ISchemaDeclarationSyntax> Types,
    SyntaxList<MessageDeclarationSyntax> Messages,
    SyntaxToken BraceCloseToken)
    : IDeclarationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.ProtocolDeclaration;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return ProtocolKeyword;
        yield return Name;
        foreach (var documentation in Documentation)
            yield return documentation;
        foreach (var annotation in Annotations)
            yield return annotation;
        yield return BraceOpenToken;
        foreach (var type in Types)
            yield return type;
        foreach (var message in Messages)
            yield return message;
        yield return BraceCloseToken;
    }
}
