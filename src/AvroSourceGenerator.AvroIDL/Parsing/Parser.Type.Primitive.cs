using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Types;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static PrimitiveTypeSyntax ParsePrimitiveType(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var typeKeyword = iterator.Match([
            SyntaxKind.VoidKeyword,
            SyntaxKind.NullKeyword,
            SyntaxKind.IntKeyword,
            SyntaxKind.LongKeyword,
            SyntaxKind.StringKeyword,
            SyntaxKind.BooleanKeyword,
            SyntaxKind.FloatKeyword,
            SyntaxKind.DoubleKeyword,
            SyntaxKind.BytesKeyword,
        ]);

        return new PrimitiveTypeSyntax(syntaxTree, typeKeyword);
    }
}
