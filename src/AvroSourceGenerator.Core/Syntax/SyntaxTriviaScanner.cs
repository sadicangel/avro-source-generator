using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Syntax;

internal static class SyntaxTriviaScanner
{
    private const int CancellationCheckInterval = 1024;

    public static int Skip(
        SourceText sourceText,
        int offset,
        List<AvroDiagnostic> diagnostics,
        CancellationToken cancellationToken,
        bool allowComments)
    {
        var sourceCode = sourceText.Text.AsSpan(offset);
        var totalSkipped = 0;
        int skipped;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            skipped = sourceCode switch
            {
                ['/', '/' or '*', ..] when !allowComments => SkipDisallowedComment(sourceText, offset + totalSkipped, sourceCode, diagnostics, cancellationToken),
                ['/', '*', not '*', ..] => SkipMultiLineComment(sourceText, offset + totalSkipped, sourceCode, diagnostics, cancellationToken),
                ['/', '/', ..] => SkipSingleLineComment(sourceText, offset + totalSkipped, sourceCode, diagnostics, cancellationToken),
                [' ' or '\t' or '\r' or '\n', ..] => SkipWhiteSpace(sourceCode, cancellationToken),
                _ => 0
            };
            totalSkipped += skipped;
            sourceCode = sourceCode[skipped..];
        } while (skipped != 0);

        return totalSkipped;
    }

    private static int SkipDisallowedComment(SourceText sourceText, int offset, ReadOnlySpan<char> sourceCode, List<AvroDiagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var length = sourceCode[1] == '/'
            ? SkipSingleLineComment(sourceText, offset, sourceCode, diagnostics: null, cancellationToken)
            : SkipMultiLineComment(sourceText, offset, sourceCode, diagnostics: null, cancellationToken);
        diagnostics.Add(AvroDiagnostic.JsonCommentNotAllowed(sourceText.GetSourceSpan(offset, length)));
        return length;
    }

    private static int SkipMultiLineComment(SourceText sourceText, int offset, ReadOnlySpan<char> sourceCode, List<AvroDiagnostic>? diagnostics, CancellationToken cancellationToken)
    {
        var done = false;
        var terminated = false;
        // Skip '/*'.
        var length = 2;
        while (!done)
        {
            if (length % CancellationCheckInterval == 0)
                cancellationToken.ThrowIfCancellationRequested();

            switch (sourceCode[length..])
            {
                case []:
                case ['\0', ..] when diagnostics is not null:
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
            diagnostics?.Add(AvroDiagnostic.UnterminatedComment(new SourceSpan(sourceText, offset, length)));

        return length;
    }

    private static int SkipSingleLineComment(SourceText sourceText, int offset, ReadOnlySpan<char> sourceCode, List<AvroDiagnostic>? diagnostics, CancellationToken cancellationToken)
    {
        // Skip '//'.
        var length = 2;
        while (length < sourceCode.Length && sourceCode[length] is not '\r' and not '\n')
        {
            if (length % CancellationCheckInterval == 0)
                cancellationToken.ThrowIfCancellationRequested();
            if (diagnostics is not null && sourceCode[length] is '\0')
                break;
            if (sourceCode[length] is '\u2028' or '\u2029')
                diagnostics?.Add(AvroDiagnostic.InvalidCharacter(sourceText.GetSourceSpan(offset + length, 1)));
            length++;
        }

        return length;
    }

    private static int SkipWhiteSpace(ReadOnlySpan<char> sourceCode, CancellationToken cancellationToken)
    {
        var length = 0;
        while (length < sourceCode.Length && sourceCode[length] is ' ' or '\t' or '\r' or '\n')
        {
            if (length % CancellationCheckInterval == 0)
                cancellationToken.ThrowIfCancellationRequested();

            length++;
        }

        return length;
    }
}
