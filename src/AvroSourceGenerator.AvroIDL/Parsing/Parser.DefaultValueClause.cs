using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static DefaultValueClauseSyntax? ParseDefaultValueClause(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        if (iterator.TryMatch(out var equalsToken, SyntaxKind.EqualsToken))
        {
            var jsonValue = ParseJsonValue(syntaxTree, iterator);
            return new DefaultValueClauseSyntax(syntaxTree, equalsToken, jsonValue);
        }

        return null;
    }
}
