using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static ErrorDeclarationSyntax ParseErrorDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var errorKeyword = iterator.Match(SyntaxKind.ErrorKeyword);
        var name = ParseSimpleName(syntaxTree, iterator);
        var braceOpenToken = iterator.Match(SyntaxKind.BraceOpenToken);
        var fields = ParseSyntaxList(syntaxTree, iterator, [SyntaxKind.BraceCloseToken], ParseFieldDeclaration);
        var braceCloseToken = iterator.Match(SyntaxKind.BraceCloseToken);

        return new ErrorDeclarationSyntax(
            syntaxTree,
            errorKeyword,
            name,
            braceOpenToken,
            fields,
            braceCloseToken);
    }
}
