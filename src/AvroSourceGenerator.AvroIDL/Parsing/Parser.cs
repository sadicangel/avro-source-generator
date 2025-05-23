using AvroSourceGenerator.AvroIDL.Scanning;
using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Parsing;

internal static partial class Parser
{
    private delegate T ParseNode<out T>(SyntaxTree syntaxTree, SyntaxIterator iterator) where T : SyntaxNode;
    private delegate T? ParseNodeOptional<out T>(SyntaxTree syntaxTree, SyntaxIterator iterator) where T : SyntaxNode;

    internal static CompilationUnitSyntax Parse(SyntaxTree syntaxTree)
    {
        var tokens = Scanner.Scan(syntaxTree).ToArray();
        if (tokens.Length == 0)
            return new CompilationUnitSyntax(syntaxTree, [], SyntaxToken.CreateSynthetic(SyntaxKind.EofToken, syntaxTree));

        var iterator = new SyntaxIterator(tokens);

        var expressions = ParseSyntaxList<SyntaxNode>(syntaxTree, iterator, [SyntaxKind.EofToken], ParseDeclaration);

        var eofToken = iterator.Match(SyntaxKind.EofToken);

        return new CompilationUnitSyntax(syntaxTree, expressions, eofToken);
    }
}
