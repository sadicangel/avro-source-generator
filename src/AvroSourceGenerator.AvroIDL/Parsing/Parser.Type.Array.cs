using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Types;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static ArrayTypeSyntax ParseArrayType(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var arrayKeyword = iterator.Match(SyntaxKind.ArrayKeyword);
        var lessThanToken = iterator.Match(SyntaxKind.LessThanToken);
        var elementType = ParseType(syntaxTree, iterator);
        var greaterThanToken = iterator.Match(SyntaxKind.GreaterThanToken);

        return new ArrayTypeSyntax(syntaxTree, arrayKeyword, lessThanToken, elementType, greaterThanToken);
    }
}
