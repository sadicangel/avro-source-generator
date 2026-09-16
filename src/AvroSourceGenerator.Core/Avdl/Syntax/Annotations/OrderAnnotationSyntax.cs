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

    // TODO: We probably want to validate that only 'ascending', 'descending' and 'ignore' are allowed.
    public string Order => field ??= JsonValue.GetRequiredString("Order annotation value");

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return AtSignToken;
        yield return AnnotationName;
        yield return ParenthesisOpenToken;
        yield return JsonValue;
        yield return ParenthesisCloseToken;
    }
}
