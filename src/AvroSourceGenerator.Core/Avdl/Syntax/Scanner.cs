using System.Globalization;
using System.Text;

namespace AvroSourceGenerator.Avdl.Syntax;

public sealed class Scanner(SourceText sourceText)
{
    private readonly List<SyntaxToken> _badTokens = [];
    private int _position = 0;
    private SyntaxToken? _previousSyntaxToken = null;

    private ReadOnlySpan<char> CurrentSpan => sourceText.Text.AsSpan(_position);

    public IReadOnlyList<SyntaxToken> BadTokens => _badTokens;

    public IEnumerable<SyntaxToken> ScanAllTokens()
    {
        while (true)
        {
            var token = Scan();
            yield return token;
            if (token.SyntaxKind == SyntaxKind.EofToken)
                break;
        }
    }

    public SyntaxToken Scan()
    {
        while (true)
        {
            _position += SyntaxTriviaScanner.Skip(CurrentSpan);
            var syntaxToken = _previousSyntaxToken = ScanAny();
            _position += syntaxToken.SourceSpan.Length;

            if (syntaxToken.SyntaxKind is not SyntaxKind.InvalidSyntax)
            {
                return syntaxToken;
            }

            _badTokens.Add(syntaxToken);
        }
    }

    private SyntaxToken ScanAny()
    {
        switch (CurrentSpan)
        {
            case ['{', ..]:
                return new SyntaxToken(SyntaxKind.BraceOpenToken, new SourceSpan(sourceText, _position, 1));

            case ['}', ..]:
                return new SyntaxToken(SyntaxKind.BraceCloseToken, new SourceSpan(sourceText, _position, 1));

            case ['(', ..]:
                return new SyntaxToken(SyntaxKind.ParenthesisOpenToken, new SourceSpan(sourceText, _position, 1));

            case [')', ..]:
                return new SyntaxToken(SyntaxKind.ParenthesisCloseToken, new SourceSpan(sourceText, _position, 1));

            case ['[', ..]:
                return new SyntaxToken(SyntaxKind.BracketOpenToken, new SourceSpan(sourceText, _position, 1));

            case [']', ..]:
                return new SyntaxToken(SyntaxKind.BracketCloseToken, new SourceSpan(sourceText, _position, 1));

            case ['<', ..]:
                return new SyntaxToken(SyntaxKind.LessThanToken, new SourceSpan(sourceText, _position, 1));

            case ['>', ..]:
                return new SyntaxToken(SyntaxKind.GreaterThanToken, new SourceSpan(sourceText, _position, 1));

            case ['@', ..]:
                return new SyntaxToken(SyntaxKind.AtSignToken, new SourceSpan(sourceText, _position, 1));

            case [',', ..]:
                return new SyntaxToken(SyntaxKind.CommaToken, new SourceSpan(sourceText, _position, 1));

            case ['.']:
            case ['.', not ('0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9'), ..]:
                return new SyntaxToken(SyntaxKind.DotToken, new SourceSpan(sourceText, _position, 1));

            case [':', ..]:
                return new SyntaxToken(SyntaxKind.ColonToken, new SourceSpan(sourceText, _position, 1));

            case [';', ..]:
                return new SyntaxToken(SyntaxKind.SemicolonToken, new SourceSpan(sourceText, _position, 1));

            case ['=', ..]:
                return new SyntaxToken(SyntaxKind.EqualsToken, new SourceSpan(sourceText, _position, 1));

            case ['?', ..]:
                return new SyntaxToken(SyntaxKind.QuestionMarkToken, new SourceSpan(sourceText, _position, 1));

            case ['/', '*', '*', ..]:
                return ScanDocumentation();

            case ['"', ..]:
                return ScanString();

            case [var d1, ..] when IsAsciiDigit(d1):
            case ['.', var d2, ..] when IsAsciiDigit(d2):
            case ['-', var d3, ..] when IsAsciiDigit(d3):
            case ['-', '.', var d4, ..] when IsAsciiDigit(d4):
                return ScanNumber();

            case [var l1, ..] when IsIdentifierStart(l1):
            case ['`', var l2, ..] when IsIdentifierStart(l2):
                return ScanIdentifier();

            // Control
            case []:
                return new SyntaxToken(SyntaxKind.EofToken, new SourceSpan(sourceText, _position, 0));

            default:
                // TODO: Report invalid character diagnostic.
                // syntaxTree.Diagnostics.ReportInvalidCharacter(span, span.Text[0]);
                return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, _position, 1));
        }
    }

    private SyntaxToken ScanDocumentation()
    {
        var start = _position;
        _position += 3; // Skip '/**'.

        var length = 0;
        while (true)
        {
            switch (CurrentSpan[length..])
            {
                case [] or ['\0', ..]:
                    // TODO: Report unterminated documentation.
                    // Diagnostics.ReportUnterminatedDocumentation(new SourceSpan(CurrentSpan.SourceText, offset, length));
                    _position = start;
                    return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, start, sourceText.Text.Length - start));

                case ['*', '/', ..]:
                    var syntaxToken = new SyntaxToken(SyntaxKind.DocumentationTrivia, new SourceSpan(sourceText, _position, length));
                    _position += 2; // Skip '*/'.
                    return syntaxToken;

                default:
                    length++;
                    break;
            }
        }
    }

    private SyntaxToken ScanString()
    {
        var builder = new StringBuilder();
        var length = 1;
        while (true)
        {
            switch (CurrentSpan[length..])
            {
                case ['\0', ..]:
                case ['\r', ..]:
                case ['\n', ..]:
                case []:
                    // TODO: Report unterminated string diagnostic.
                    // syntaxTree.Diagnostics.ReportUnterminatedString(new SourceSpan(syntaxTree.SourceText, offset, length));
                    return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, _position, length));
                case ['\\', '"', ..]:
                    builder.Append('"');
                    length += 2;
                    break;
                case ['\\', '\\', ..]:
                    builder.Append('\\');
                    length += 2;
                    break;
                case ['\\', '/', ..]:
                    builder.Append('/');
                    length += 2;
                    break;
                case ['\\', 'b', ..]:
                    builder.Append('\b');
                    length += 2;
                    break;
                case ['\\', 'f', ..]:
                    builder.Append('\f');
                    length += 2;
                    break;
                case ['\\', 'n', ..]:
                    builder.Append('\n');
                    length += 2;
                    break;
                case ['\\', 'r', ..]:
                    builder.Append('\r');
                    length += 2;
                    break;
                case ['\\', 't', ..]:
                    builder.Append('\t');
                    length += 2;
                    break;
                case ['\\', 'u', var h1, var h2, var h3, var h4, ..] when IsHexDigit(h1) && IsHexDigit(h2) && IsHexDigit(h3) && IsHexDigit(h4):
                    builder.Append((char)((HexValue(h1) << 12) + (HexValue(h2) << 8) + (HexValue(h3) << 4) + HexValue(h4)));
                    length += 6;
                    break;
                case ['\\', ..]:
                    return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, _position, GetInvalidStringLength(length + 2)));
                case ['"', ..]:
                    return new SyntaxToken(SyntaxKind.StringLiteralToken, new SourceSpan(sourceText, _position, length + 1), builder.ToString());
                default:
                    builder.Append(CurrentSpan[length]);
                    length++;
                    break;
            }
        }
    }

    private static bool IsAsciiDigit(char c) => c is >= '0' and <= '9';

    private static bool IsHexDigit(char c) => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        _ => c - 'A' + 10,
    };

    private SyntaxToken ScanNumber()
    {
        var length = 0;
        var isFloat = false;

        if (CurrentSpan[length] is '-')
            length++;

        var wholeDigitsStart = length;
        while (length < CurrentSpan.Length && IsAsciiDigit(CurrentSpan[length]))
        {
            ++length;
        }

        var hasWholeDigits = length > wholeDigitsStart;
        if (length < CurrentSpan.Length && CurrentSpan[length] is '.')
        {
            isFloat = true;
            ++length;
            var fractionDigitsStart = length;
            while (length < CurrentSpan.Length && IsAsciiDigit(CurrentSpan[length]))
            {
                ++length;
            }

            if (!hasWholeDigits && length == fractionDigitsStart)
            {
                return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, _position, length));
            }
        }

        if (length < CurrentSpan.Length && CurrentSpan[length] is 'e' or 'E')
        {
            isFloat = true;
            ++length;
            if (length < CurrentSpan.Length && CurrentSpan[length] is '+' or '-')
                ++length;

            var exponentDigitsStart = length;
            while (length < CurrentSpan.Length && IsAsciiDigit(CurrentSpan[length]))
            {
                ++length;
            }

            if (length == exponentDigitsStart)
            {
                return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, _position, length));
            }
        }

        if (!hasWholeDigits && CurrentSpan[0] is not '.')
        {
            return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, _position, Math.Max(length, 1)));
        }

        SyntaxKind syntaxKind;
        object value;
        if (isFloat)
        {
            syntaxKind = SyntaxKind.FloatLiteralToken;
            if (!double.TryParse(CurrentSpan[..length].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var @float))
                return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, _position, length));

            value = @float;
        }
        else
        {
            syntaxKind = SyntaxKind.IntegerLiteralToken;
            if (!long.TryParse(CurrentSpan[..length].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var @int))
                return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, _position, length));

            value = @int is >= int.MinValue and <= int.MaxValue ? (object)(int)@int : @int;
        }

        return new SyntaxToken(syntaxKind, new SourceSpan(sourceText, _position, length), value);
    }

    private int GetInvalidStringLength(int start)
    {
        var length = Math.Min(start, CurrentSpan.Length);
        while (length < CurrentSpan.Length)
        {
            switch (CurrentSpan[length])
            {
                case '"':
                    return length + 1;
                case '\r':
                case '\n':
                case '\0':
                    return length;
                default:
                    length++;
                    break;
            }
        }

        return length;
    }

    private SyntaxToken ScanIdentifier()
    {
        var start = _position;
        var isVerbatim = CurrentSpan is ['`', ..];

        var identifierSpan = isVerbatim ? CurrentSpan[1..] : CurrentSpan;

        if (identifierSpan.IsEmpty || !IsIdentifierStart(identifierSpan[0]))
        {
            return new SyntaxToken(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, start, 1));
        }

        var length = GetIdentifierLength(identifierSpan, _previousSyntaxToken?.SyntaxKind is SyntaxKind.AtSignToken);

        if (isVerbatim)
        {
            if (length >= identifierSpan.Length || identifierSpan[length] is not '`')
            {
                return new SyntaxToken(
                    SyntaxKind.InvalidSyntax,
                    new SourceSpan(
                        sourceText,
                        start,
                        Math.Min(sourceText.Text.Length - start, length + 1)));
            }

            return new SyntaxToken(
                SyntaxKind.IdentifierToken,
                new SourceSpan(sourceText, start, length + 2),
                identifierSpan[..length].ToString());
        }

        var text = identifierSpan[..length];
        var kind = SyntaxFacts.GetKeywordKind(text);

        object? value = kind switch
        {
            SyntaxKind.TrueKeyword => true,
            SyntaxKind.FalseKeyword => false,
            SyntaxKind.IdentifierToken => text.ToString(),
            _ => null,
        };

        return new SyntaxToken(kind, new SourceSpan(sourceText, start, length), value);
    }

    private static int GetIdentifierLength(ReadOnlySpan<char> chars, bool allowDash)
    {
        var length = 0;
        while (length < chars.Length && IsValid(chars[length], allowDash))
        {
            length++;
        }

        return length;

        static bool IsValid(char c, bool allowDash) => c is '_' || IsAsciiDigit(c) || IsAsciiLetter(c) || (allowDash && c == '-');
    }

    private static bool IsIdentifierStart(char c) => c is '_' || IsAsciiLetter(c);

    private static bool IsAsciiLetter(char c) => (uint)((c | 0x20) - 'a') <= 'z' - 'a';
}
