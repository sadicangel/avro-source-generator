using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static SimpleNameSyntax ParseSimpleName(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var identifierToken = iterator.Match(SyntaxKind.IdentifierToken);

        return new SimpleNameSyntax(syntaxTree, identifierToken);
    }
}
