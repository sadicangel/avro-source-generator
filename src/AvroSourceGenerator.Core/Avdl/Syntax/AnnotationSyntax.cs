namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class AnnotationSyntax(
    SyntaxToken AtSignToken,
    SyntaxToken NameIdentifier,
    SyntaxToken ParenthesisOpenToken,
    JsonValueSyntax? Json,
    SyntaxToken ParenthesisCloseToken)
    : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.Annotation;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return AtSignToken;
        yield return NameIdentifier;
        yield return ParenthesisOpenToken;
        if (Json is not null) yield return Json;
        yield return ParenthesisCloseToken;
    }
}
