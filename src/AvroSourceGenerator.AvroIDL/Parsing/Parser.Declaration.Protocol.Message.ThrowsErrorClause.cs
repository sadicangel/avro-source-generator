using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static ThrowsErrorClauseSyntax? ParseThrowsErrorClause(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        if (iterator.TryMatch(out var throwsKeyword, SyntaxKind.ThrowsKeyword))
        {
            var errors = ParseSyntaxList(syntaxTree, iterator, SyntaxKind.CommaToken, [SyntaxKind.SemicolonToken], ParseNamedType);

            return new ThrowsErrorClauseSyntax(syntaxTree, throwsKeyword, errors);
        }

        return null;
    }
}
