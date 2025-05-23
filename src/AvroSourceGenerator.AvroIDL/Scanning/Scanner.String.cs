using System.Text;
using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static int ScanString(SyntaxTree syntaxTree, int offset, out SyntaxKind kind, out SourceSpan span, out object? value)
    {
        var builder = new StringBuilder();
        var done = false;
        var length = 1;
        while (!done)
        {
            var head = syntaxTree.SourceText.Text.AsSpan(offset + length);
            switch (head)
            {
                case ['\0', ..]:
                case ['\r', ..]:
                case ['\n', ..]:
                case []:
                    syntaxTree.Diagnostics.ReportUnterminatedString(new SourceSpan(syntaxTree.SourceText, offset, length));
                    done = true;
                    break;
                case ['\\', '"', ..]:
                    length++;
                    builder.Append(head[1]);
                    length++;
                    break;
                case ['"', ..]:
                    length++;
                    done = true;
                    break;
                default:
                    builder.Append(head[0]);
                    length++;
                    break;
            }
        }

        kind = SyntaxKind.StringLiteralToken;
        span = new SourceSpan(syntaxTree.SourceText, offset, length);
        value = builder.ToString();
        return length;
    }
}
