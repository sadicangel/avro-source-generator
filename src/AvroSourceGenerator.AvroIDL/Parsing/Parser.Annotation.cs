using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static AnnotationSyntax? ParseAnnotation(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        if (iterator.TryMatch(out var atSignToken, SyntaxKind.AtSignToken))
        {
            var name = ParseSimpleName(syntaxTree, iterator);
            var parenthesisOpenToken = iterator.Match(SyntaxKind.ParenthesisOpenToken);
            var jsonValue = ParseJsonValue(syntaxTree, iterator);
            var parenthesisCloseToken = iterator.Match(SyntaxKind.ParenthesisCloseToken);

            return new AnnotationSyntax(
                syntaxTree,
                atSignToken,
                name,
                parenthesisOpenToken,
                jsonValue,
                parenthesisCloseToken);
        }

        return null;
    }
}
