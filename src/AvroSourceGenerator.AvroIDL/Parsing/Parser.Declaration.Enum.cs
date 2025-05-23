using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static EnumDeclarationSyntax ParseEnumDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var enumKeyword = iterator.Match(SyntaxKind.EnumKeyword);
        var name = ParseSimpleName(syntaxTree, iterator);
        var braceOpenToken = iterator.Match(SyntaxKind.BraceOpenToken);
        var symbols = ParseSyntaxList(syntaxTree, iterator, SyntaxKind.CommaToken, [SyntaxKind.BraceCloseToken], ParseSimpleName);
        var braceCloseToken = iterator.Match(SyntaxKind.BraceCloseToken);

        return new EnumDeclarationSyntax(
            syntaxTree,
            enumKeyword,
            name,
            braceOpenToken,
            symbols,
            braceCloseToken);
    }
}
