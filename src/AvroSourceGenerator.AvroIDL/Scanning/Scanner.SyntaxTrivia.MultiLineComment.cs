using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static int ScanMultiLineComment(SyntaxTree syntaxTree, int offset, out SyntaxTrivia trivia)
    {
        var done = false;
        // Skip '/*'.
        var length = 2;
        while (!done)
        {
            switch (syntaxTree.SourceText.Text.AsSpan(offset + length))
            {
                case []:
                case ['\0', ..]:
                    syntaxTree.Diagnostics.ReportUnterminatedComment(new SourceSpan(syntaxTree.SourceText, offset, length));
                    done = true;
                    break;
                case ['*', '/', ..]:
                    length += 2;
                    done = true;
                    break;
                default:
                    length++;
                    break;
            }
        }

        trivia = new SyntaxTrivia(SyntaxKind.MultiLineCommentTrivia, syntaxTree, new SourceSpan(syntaxTree.SourceText, offset, length));

        return length;
    }
}
