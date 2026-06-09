using System.Collections.Immutable;

namespace AvroSourceGenerator.Avdl.Syntax.Annotations;

public sealed record class AliasesAnnotationSyntax(
    SyntaxToken AtSignToken,
    AnnotationNameSyntax AnnotationName,
    SyntaxToken ParenthesisOpenToken,
    JsonValueSyntax JsonValue,
    SyntaxToken ParenthesisCloseToken)
    : IAnnotationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.AliasesAnnotation;

    public ImmutableArray<string> Aliases => !field.IsDefault ? field : field = JsonValue.JsonNode?.AsArray().GetValues<string>().ToImmutableArray() ?? [];

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return AtSignToken;
        yield return AnnotationName;
        yield return ParenthesisOpenToken;
        yield return JsonValue;
        yield return ParenthesisCloseToken;
    }
}
