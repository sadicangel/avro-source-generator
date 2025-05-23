using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static FieldDeclarationSyntax ParseFieldDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var type = ParseType(syntaxTree, iterator);
        var name = ParseSimpleName(syntaxTree, iterator);
        var defaultValue = ParseDefaultValueClause(syntaxTree, iterator);
        var semicolonToken = iterator.Match(SyntaxKind.SemicolonToken);

        return new FieldDeclarationSyntax(
            syntaxTree,
            type,
            name,
            defaultValue,
            semicolonToken);
    }
}
