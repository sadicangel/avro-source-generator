using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Parsing;

internal readonly record struct LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public static readonly LocationInfo None = default;

    public static LocationInfo FromSourceFile(string filePath, string? text)
    {
        return new LocationInfo(filePath, new TextSpan(0, text?.Length ?? 0), new LinePositionSpan(LinePosition.Zero, GetLastLinePosition(text.AsSpan())));

        static LinePosition GetLastLinePosition(ReadOnlySpan<char> text)
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

    public static LocationInfo FromException(string filePath, string? text, JsonException exception)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return FromSourceFile(filePath, text);
        }

        var sourceText = SourceText.From(text!);
        var lineNumber = exception.LineNumber ?? 0;
        var bytePositionInLine = exception.BytePositionInLine ?? 0;

        var line = sourceText.Lines[Math.Min((int)lineNumber, sourceText.Lines.Count - 1)];
        var charIndex = Math.Min((int)bytePositionInLine, line.Span.Length);

        var span = new TextSpan(line.Start + charIndex, line.End);
        var lineSpan = sourceText.Lines.GetLinePositionSpan(span);

        return new LocationInfo(filePath, span, lineSpan);
    }

    private Location ToLocation() => string.IsNullOrWhiteSpace(FilePath) ? Location.None : Location.Create(FilePath, TextSpan, LineSpan);

    public static implicit operator Location(LocationInfo location) => location.ToLocation();
}
