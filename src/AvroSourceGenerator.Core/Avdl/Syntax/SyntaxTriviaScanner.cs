using AvroSourceGenerator.Avdl.Diagnostics;
using AvroSourceGenerator.Avdl.Text;

namespace AvroSourceGenerator.Avdl.Syntax;

internal static class SyntaxTriviaScanner
{
    public static int Skip(SourceText sourceText, int offset, List<SyntaxDiagnostic> diagnostics)
    {
        var sourceCode = sourceText.Text.AsSpan(offset);
        var totalSkipped = 0;
        var skipped = 0;
        do
        {
            skipped = sourceCode switch
            {
                ['/', '*', not '*', ..] => SkipMultiLineComment(sourceText, offset + totalSkipped, sourceCode, diagnostics),
                ['/', '/', ..] => SkipSingleLineComment(sourceCode),
                ['\n' or '\r', ..] => SkipLineBreak(sourceCode),
                [' ' or '\t', ..] => SkipWhiteSpace(sourceCode),
                [var whitespace, ..] when char.IsWhiteSpace(whitespace) => SkipWhiteSpace(sourceCode),
                _ => 0
            };
            totalSkipped += skipped;
            sourceCode = sourceCode[skipped..];
        } while (skipped != 0);

        return totalSkipped;
    }

    private static int SkipLineBreak(ReadOnlySpan<char> sourceCode)
    {
        var length = 0;
        if (sourceCode is ['\r', '\n', ..])
            length++;
        length++;

        return length;
    }

    private static int SkipMultiLineComment(SourceText sourceText, int offset, ReadOnlySpan<char> sourceCode, List<SyntaxDiagnostic> diagnostics)
    {
        var done = false;
        var terminated = false;
        // Skip '/*'.
        var length = 2;
        while (!done)
        {
            switch (sourceCode[length..])
            {
                case []:
                case ['\0', ..]:
                    done = true;
                    break;
                case ['*', '/', ..]:
                    length += 2;
                    terminated = true;
                    done = true;
                    break;
                default:
                    length++;
                    break;
            }
        }

        if (!terminated)
            diagnostics.Add(SyntaxDiagnostic.UnterminatedComment(new SourceSpan(sourceText, offset, length)));

        return length;
    }

    private static int SkipSingleLineComment(ReadOnlySpan<char> sourceCode)
    {
        // Skip '//'.
        var length = 2;
        while (length < sourceCode.Length && sourceCode[length] is not '\r' and not '\n' and not '\0')
            length++;

        return length;
    }

    private static int SkipWhiteSpace(ReadOnlySpan<char> sourceCode)
    {
        var done = false;
        var length = 0;
        while (!done)
        {
            switch (sourceCode[length..])
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

        return length;
    }
}
