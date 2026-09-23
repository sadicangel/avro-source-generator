namespace AvroSourceGenerator.Avdl.Annotations;

public sealed record class LogicalTypeAnnotationSyntax(
    SyntaxToken AtSignToken,
    AnnotationNameSyntax AnnotationName,
    SyntaxToken ParenthesisOpenToken,
    JsonValueSyntax JsonValue,
    SyntaxToken ParenthesisCloseToken)
    : IAnnotationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.LogicalTypeAnnotation;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return AtSignToken;
        yield return AnnotationName;
        yield return ParenthesisOpenToken;
        yield return JsonValue;
        yield return ParenthesisCloseToken;
    }
}
