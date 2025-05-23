using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static MessageDeclarationSyntax ParseMessageDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var type = ParseType(syntaxTree, iterator);
        var name = ParseSimpleName(syntaxTree, iterator);
        var parenthesisOpenToken = iterator.Match(SyntaxKind.ParenthesisOpenToken);
        var parameters = ParseSyntaxList(syntaxTree, iterator, [SyntaxKind.ParenthesisCloseToken], ParseParameterDeclaration);
        var parenthesisCloseToken = iterator.Match(SyntaxKind.ParenthesisCloseToken);
        var oneWayClause = ParseOneWayClause(syntaxTree, iterator);
        var throwsErrorClause = ParseThrowsErrorClause(syntaxTree, iterator);
        var semicolonToken = iterator.Match(SyntaxKind.SemicolonToken);

        return new MessageDeclarationSyntax(
            syntaxTree,
            type,
            name,
            parenthesisOpenToken,
            parameters,
            parenthesisCloseToken,
            oneWayClause,
            throwsErrorClause,
            semicolonToken);
    }
}
