using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static int ScanSingleLineComment(SyntaxTree syntaxTree, int offset, out SyntaxTrivia trivia)
    {
        // Skip '//'.
        var length = 2;
        while (syntaxTree.SourceText[offset + length] is not '\r' and not '\n' and not '\0')
            length++;

        trivia = new SyntaxTrivia(SyntaxKind.SingleLineCommentTrivia, syntaxTree, new SourceSpan(syntaxTree.SourceText, offset, length));

        return length;
    }
}
