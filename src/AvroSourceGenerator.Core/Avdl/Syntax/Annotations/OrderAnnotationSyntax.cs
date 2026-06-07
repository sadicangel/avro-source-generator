namespace AvroSourceGenerator.Avdl.Syntax.Annotations;

public sealed record class OrderAnnotationSyntax(
    SyntaxToken AtSignToken,
    SyntaxToken OrderIdentifier,
    SyntaxToken ParenthesisOpenToken,
    JsonValueSyntax JsonValue,
    SyntaxToken ParenthesisCloseToken)
    : IAnnotationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.OrderAnnotation;

    // TODO: We probably want to validate that only 'ascending', 'descending' and 'ignore' are allowed.
    public string Order => field ??= JsonValue.JsonNode?.GetValue<string>() ?? "ignore";

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return AtSignToken;
        yield return OrderIdentifier;
        yield return ParenthesisOpenToken;
        yield return JsonValue;
        yield return ParenthesisCloseToken;
    }
}
