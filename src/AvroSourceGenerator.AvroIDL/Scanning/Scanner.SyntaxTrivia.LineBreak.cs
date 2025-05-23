using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static int ScanLineBreak(SyntaxTree syntaxTree, int offset, out SyntaxTrivia trivia)
    {
        var length = 0;
        if (syntaxTree.SourceText.Text.AsSpan(offset + length) is ['\r', '\n', ..])
            length++;
        length++;

        trivia = new SyntaxTrivia(SyntaxKind.LineBreakTrivia, syntaxTree, new SourceSpan(syntaxTree.SourceText, offset, length));

        return length;
    }
}
