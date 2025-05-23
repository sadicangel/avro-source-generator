using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static FixedDeclarationSyntax ParseFixedDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var fixedKeyword = iterator.Match(SyntaxKind.FixedKeyword);
        var name = ParseSimpleName(syntaxTree, iterator);
        var parenthesisOpenToken = iterator.Match(SyntaxKind.ParenthesisOpenToken);
        var sizeLiteralToken = iterator.Match(SyntaxKind.IntegerLiteralToken);
        var parenthesisCloseToken = iterator.Match(SyntaxKind.ParenthesisCloseToken);

        return new FixedDeclarationSyntax(
            syntaxTree,
            fixedKeyword,
            name,
            parenthesisOpenToken,
            sizeLiteralToken,
            parenthesisCloseToken);
    }
}
