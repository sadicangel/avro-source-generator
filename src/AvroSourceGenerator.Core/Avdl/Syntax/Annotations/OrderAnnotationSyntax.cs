namespace AvroSourceGenerator.Avdl.Syntax.Annotations;

public sealed record class OrderAnnotationSyntax(
    SyntaxToken AtSignToken,
    AnnotationNameSyntax AnnotationName,
    SyntaxToken ParenthesisOpenToken,
    JsonValueSyntax JsonValue,
    SyntaxToken ParenthesisCloseToken)
    : IAnnotationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.OrderAnnotation;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return AtSignToken;
        yield return AnnotationName;
        yield return ParenthesisOpenToken;
        yield return JsonValue;
        yield return ParenthesisCloseToken;
    }
}
