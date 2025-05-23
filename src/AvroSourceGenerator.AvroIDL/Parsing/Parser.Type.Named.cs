using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Types;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static NamedTypeSyntax ParseNamedType(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var name = ParseName(syntaxTree, iterator);

        return new NamedTypeSyntax(syntaxTree, name);
    }
}
