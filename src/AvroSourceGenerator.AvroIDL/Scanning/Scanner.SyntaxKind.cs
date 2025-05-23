using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static int ScanSyntaxKind(SyntaxTree syntaxTree, int offset, out SyntaxKind kind, out SourceSpan span, out object? value)
    {
        var head = syntaxTree.SourceText.Text.AsSpan(offset);
        switch (head)
        {
            case ['{', ..]:
                kind = SyntaxKind.BraceOpenToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['}', ..]:
                kind = SyntaxKind.BraceCloseToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['(', ..]:
                kind = SyntaxKind.ParenthesisOpenToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case [')', ..]:
                kind = SyntaxKind.ParenthesisCloseToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['[', ..]:
                kind = SyntaxKind.BracketOpenToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case [']', ..]:
                kind = SyntaxKind.BracketCloseToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['<', ..]:
                kind = SyntaxKind.LessThanToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['>', ..]:
                kind = SyntaxKind.GreaterThanToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['@', ..]:
                kind = SyntaxKind.AtSignToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case [',', ..]:
                kind = SyntaxKind.CommaToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['.']:
            case ['.', not ('0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9'), ..]:
                kind = SyntaxKind.DotToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case [':', ..]:
                kind = SyntaxKind.ColonToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case [';', ..]:
                kind = SyntaxKind.SemicolonToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['=', ..]:
                kind = SyntaxKind.EqualsToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['?', ..]:
                kind = SyntaxKind.HookToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                return 1;

            case ['"', ..]:
                return ScanString(syntaxTree, offset, out kind, out span, out value);

            case [var d1, ..] when IsAsciiDigit(d1):
            case ['.', var d2, ..] when IsAsciiDigit(d2):
                return ScanNumber(syntaxTree, offset, out kind, out span, out value);

            case ['_', ..]:
            case [var l, ..] when IsAsciiLetter(l):
                return ScanIdentifier(syntaxTree, offset, out kind, out span, out value);

            // Control
            case []:
                kind = SyntaxKind.EofToken;
                span = new SourceSpan(syntaxTree.SourceText, offset, 0);
                value = null;
                return 0;

            default:
                kind = SyntaxKind.InvalidSyntax;
                span = new SourceSpan(syntaxTree.SourceText, offset, 1);
                value = null;
                syntaxTree.Diagnostics.ReportInvalidCharacter(span, span.Text[0]);
                return 1;
        }
    }
}
