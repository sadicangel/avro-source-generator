using System.Globalization;
using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static bool IsAsciiDigit(char c) => c is >= '0' and <= '9';

    private static int ScanNumber(SyntaxTree syntaxTree, int offset, out SyntaxKind kind, out SourceSpan span, out object? value)
    {
        var length = 0;
        var isFloat = false;
        var isInvalid = false;
        var numberStyles = NumberStyles.Number;

        // TODO: Handle negative number literals.
        if (syntaxTree.SourceText[offset + length] is '-')
            length++;

        while (IsAsciiDigit(syntaxTree.SourceText[offset + length]))
        {
            ++length;
        }

        if (syntaxTree.SourceText[offset + length] is '.')
        {
            isFloat = true;
            ++length;
            while (IsAsciiDigit(syntaxTree.SourceText[offset + length]))
            {
                ++length;
            }
        }

        if (syntaxTree.SourceText[offset + length] is 'e' or 'E')
        {
            isFloat = true;
            if (!IsAsciiDigit(syntaxTree.SourceText[offset + length - 1]))
            {
                isInvalid = true;
            }
            ++length;
            while (IsAsciiDigit(syntaxTree.SourceText[offset + length]))
            {
                ++length;
            }
        }

        span = new SourceSpan(syntaxTree.SourceText, offset, length);

        if (isFloat)
        {
            kind = SyntaxKind.FloatLiteralToken;
            value = 0D;

            if (!isInvalid)
            {
                isInvalid = !double.TryParse(span.ToString(), out var @float);
                value = @float;
            }
        }
        else
        {

            kind = SyntaxKind.IntegerLiteralToken;
            value = 0;
            isInvalid = true;
            if (long.TryParse(span.ToString(), numberStyles, CultureInfo.InvariantCulture, out var @int))
            {
                isInvalid = false;
                value = @int is >= int.MinValue and <= int.MaxValue ? (int)@int : @int;
            }
        }

        if (isInvalid)
        {
            syntaxTree.Diagnostics.ReportInvalidSyntaxValue(span, kind);
        }

        return length;
    }
}
