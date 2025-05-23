using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static OneWayClauseSyntax? ParseOneWayClause(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        return iterator.TryMatch(out var oneWayKeyword, SyntaxKind.OneWayKeyword)
            ? new OneWayClauseSyntax(syntaxTree, oneWayKeyword)
            : null;
    }
}
