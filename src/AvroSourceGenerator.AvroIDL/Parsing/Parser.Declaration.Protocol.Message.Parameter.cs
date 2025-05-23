using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static ParameterDeclarationSyntax ParseParameterDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var type = ParseType(syntaxTree, iterator);
        var name = ParseSimpleName(syntaxTree, iterator);
        var defaultValueClause = ParseDefaultValueClause(syntaxTree, iterator);

        return new ParameterDeclarationSyntax(
            syntaxTree,
            type,
            name,
            defaultValueClause);
    }
}
