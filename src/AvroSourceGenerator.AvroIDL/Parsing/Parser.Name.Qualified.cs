using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static QualifiedNameSyntax ParseQualifiedName(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var left = ParseFirstQualifiedName(syntaxTree, iterator);
        while (iterator.Current.SyntaxKind is SyntaxKind.DotToken)
        {
            var colonColonToken = iterator.Match(SyntaxKind.DotToken);
            var right = ParseSimpleName(syntaxTree, iterator);

            left = new QualifiedNameSyntax(syntaxTree, left, colonColonToken, right);
        }

        return left;

        static QualifiedNameSyntax ParseFirstQualifiedName(SyntaxTree syntaxTree, SyntaxIterator iterator)
        {
            var left = ParseSimpleName(syntaxTree, iterator);
            var colonColonToken = iterator.Match(SyntaxKind.DotToken);
            var right = ParseSimpleName(syntaxTree, iterator);

            return new QualifiedNameSyntax(syntaxTree, left, colonColonToken, right);
        }
    }
}
