namespace AvroSourceGenerator.Avdl.Syntax.Annotations;

public sealed record class CustomAnnotationSyntax(
    SyntaxToken AtSignToken,
    SyntaxToken NameIdentifier,
    SyntaxToken ParenthesisOpenToken,
    JsonValueSyntax JsonValue,
    SyntaxToken ParenthesisCloseToken)
    : IAnnotationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.CustomAnnotation;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return AtSignToken;
        yield return NameIdentifier;
        yield return ParenthesisOpenToken;
        yield return JsonValue;
        yield return ParenthesisCloseToken;
    }
}
