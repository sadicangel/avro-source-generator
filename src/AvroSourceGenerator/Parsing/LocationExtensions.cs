using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Parsing;

internal static class LocationExtensions
{
    public static Location GetLocation(this string path, string? text) =>
        Location.Create(path, new TextSpan(0, text?.Length ?? 0), new LinePositionSpan(LinePosition.Zero, text.GetLastLinePosition()));

    public static Location GetLocation(this string path, TextSpan textSpan, LinePositionSpan lineSpan) =>
        Location.Create(path, textSpan, lineSpan);

    public static Location GetLocation(this string path, string? text, JsonException exception)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return path.GetLocation(text);
        }

        var sourceText = SourceText.From(text!);
        var lineNumber = exception.LineNumber ?? 0;
        var bytePositionInLine = exception.BytePositionInLine ?? 0;

        var line = sourceText.Lines[Math.Min((int)lineNumber, sourceText.Lines.Count - 1)];
        var charIndex = Math.Min((int)bytePositionInLine, line.Span.Length);

        var span = new TextSpan(line.Start + charIndex, line.End);
        var lineSpan = sourceText.Lines.GetLinePositionSpan(span);

        return path.GetLocation(span, lineSpan);
    }

    public static LinePosition GetLastLinePosition(this string? text) => text.AsSpan().GetLastLinePosition();

    public static LinePosition GetLastLinePosition(this ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return new LinePosition(0, 0);
        }

        var line = 0;
        var lastLineStart = 0;

        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i..])
            {
                case ['\n', ..]:
                    line++;
                    lastLineStart = i + 1;
                    break;
                case ['\r', '\n', ..]:
                    i++; // Skip the '\n' following the '\r'
                    line++;
                    lastLineStart = i + 1;
                    break;
            }
        }

        var character = text.Length - lastLineStart;
        return new LinePosition(line, character);
    }
}
