using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Types;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static TypeSyntax ParseType(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        return iterator.Current.SyntaxKind switch
        {
            SyntaxKind.VoidKeyword => ParsePrimitiveType(syntaxTree, iterator),
            SyntaxKind.NullKeyword => ParsePrimitiveType(syntaxTree, iterator),
            SyntaxKind.IntKeyword => ParsePrimitiveType(syntaxTree, iterator),
            SyntaxKind.LongKeyword => ParsePrimitiveType(syntaxTree, iterator),
            SyntaxKind.StringKeyword => ParsePrimitiveType(syntaxTree, iterator),
            SyntaxKind.BooleanKeyword => ParsePrimitiveType(syntaxTree, iterator),
            SyntaxKind.FloatKeyword => ParsePrimitiveType(syntaxTree, iterator),
            SyntaxKind.DoubleKeyword => ParsePrimitiveType(syntaxTree, iterator),
            SyntaxKind.BytesKeyword => ParsePrimitiveType(syntaxTree, iterator),

            SyntaxKind.ArrayKeyword => ParseArrayType(syntaxTree, iterator),
            SyntaxKind.MapKeyword => ParseMapType(syntaxTree, iterator),

            _ => ParseNamedType(syntaxTree, iterator),
        };
    }
}
