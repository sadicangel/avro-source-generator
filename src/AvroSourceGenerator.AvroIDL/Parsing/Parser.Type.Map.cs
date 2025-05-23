using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Types;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static MapTypeSyntax ParseMapType(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var mapKeyword = iterator.Match(SyntaxKind.MapKeyword);
        var lessThanToken = iterator.Match(SyntaxKind.LessThanToken);
        var valueType = ParseType(syntaxTree, iterator);
        var greaterThanToken = iterator.Match(SyntaxKind.GreaterThanToken);

        return new MapTypeSyntax(
            syntaxTree,
            mapKeyword,
            lessThanToken,
            valueType,
            greaterThanToken);
    }
}
