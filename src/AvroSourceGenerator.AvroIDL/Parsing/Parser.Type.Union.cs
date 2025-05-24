using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Types;

namespace AvroSourceGenerator.AvroIDL.Parsing;
partial class Parser
{
    private static UnionTypeSyntax ParseUnionType(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var unionKeyword = iterator.Match(SyntaxKind.UnionKeyword);
        var braceOpenToken = iterator.Match(SyntaxKind.BraceOpenToken);
        var types = ParseSyntaxList(syntaxTree, iterator, ParseType);
        var braceCloseToken = iterator.Match(SyntaxKind.BraceCloseToken);

        return new UnionTypeSyntax(
            syntaxTree,
            unionKeyword,
            braceOpenToken,
            types,
            braceCloseToken);
    }
}
