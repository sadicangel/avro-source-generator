using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static bool IsAsciiLetter(char c) => (uint)((c | 0x20) - 'a') <= 'z' - 'a';

    private static int ScanIdentifier(SyntaxTree syntaxTree, int offset, out SyntaxKind kind, out SourceSpan span, out object? value)
    {
        var length = 0;
        do
        {
            length++;
        }
        while (IsValid(syntaxTree.SourceText[offset + length]));

        span = new SourceSpan(syntaxTree.SourceText, offset, length);
        kind = SyntaxFacts.GetKeywordKind(span.Text);
        value = kind switch
        {
            SyntaxKind.TrueKeyword => true,
            SyntaxKind.FalseKeyword => false,
            _ => null,
        };
        return length;

        static bool IsValid(char c) => c is '_' || IsAsciiDigit(c) || IsAsciiLetter(c);
    }
}
