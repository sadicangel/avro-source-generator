namespace AvroSourceGenerator.Avdl.Syntax;

internal static class SyntaxTriviaScanner
{
    public static int Skip(ReadOnlySpan<char> sourceCode)
    {
        var totalSkipped = 0;
        var skipped = 0;
        do
        {
            skipped = sourceCode switch
            {
                ['/', '*', not '*', ..] => SkipMultiLineComment(sourceCode),
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

    private static int SkipMultiLineComment(ReadOnlySpan<char> sourceCode)
    {
        var done = false;
        // Skip '/*'.
        var length = 2;
        while (!done)
        {
            switch (sourceCode[length..])
            {
                case []:
                case ['\0', ..]:
                    // TODO: Report unterminated comment.
                    // sourceCode.Diagnostics.ReportUnterminatedComment(new SourceSpan(sourceCode.SourceText, offset, length));
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
