using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static int ScanWhiteSpace(SyntaxTree syntaxTree, int offset, out SyntaxTrivia trivia)
    {
        var done = false;
        var length = 0;
        while (!done)
        {
            switch (syntaxTree.SourceText.Text.AsSpan(offset + length))
            {
                case []:
                case ['\0' or '\r' or '\n', ..]:
                case [var c, ..] when !char.IsWhiteSpace(c):
                    done = true;
                    break;
                default:
                    length++;
                    break;
            }
        }

        trivia = new SyntaxTrivia(SyntaxKind.WhiteSpaceTrivia, syntaxTree, new SourceSpan(syntaxTree.SourceText, offset, length));

        return length;
    }
}
