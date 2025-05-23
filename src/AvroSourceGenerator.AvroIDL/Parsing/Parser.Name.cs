using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static NameSyntax ParseName(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        if (iterator.Peek(1).SyntaxKind is SyntaxKind.DotToken)
            return ParseQualifiedName(syntaxTree, iterator);

        return ParseSimpleName(syntaxTree, iterator);
    }
}
