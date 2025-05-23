using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static int ScanSyntaxToken(SyntaxTree syntaxTree, int position, out SyntaxToken token)
    {
        var read = 0;
        read += ScanSyntaxTrivia(syntaxTree, position + read, leading: true, out var leadingTrivia);
        read += ScanSyntaxKind(syntaxTree, position + read, out var syntaxKind, out var sourceSpan, out var value);
        read += ScanSyntaxTrivia(syntaxTree, position + read, leading: false, out var trailingTrivia);

        token = new SyntaxToken(syntaxKind, syntaxTree, sourceSpan, leadingTrivia, trailingTrivia, value);
        return read;
    }
}
