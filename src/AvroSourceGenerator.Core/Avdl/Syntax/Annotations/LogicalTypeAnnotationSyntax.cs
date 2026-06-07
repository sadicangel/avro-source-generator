namespace AvroSourceGenerator.Avdl.Syntax.Annotations;

public sealed record class LogicalTypeAnnotationSyntax(
    SyntaxToken AtSignToken,
    SyntaxToken LogicalTypeIdentifier,
    SyntaxToken ParenthesisOpenToken,
    JsonValueSyntax JsonValue,
    SyntaxToken ParenthesisCloseToken)
    : IAnnotationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.LogicalTypeAnnotation;

    public string LogicalTypeName => field ??= JsonValue.JsonNode?.GetValue<string>() ?? string.Empty;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return AtSignToken;
        yield return LogicalTypeIdentifier;
        yield return ParenthesisOpenToken;
        yield return JsonValue;
        yield return ParenthesisCloseToken;
    }
}
