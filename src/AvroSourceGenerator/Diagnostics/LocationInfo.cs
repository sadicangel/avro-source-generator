using System.Text.Json;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceText = AvroSourceGenerator.Text.SourceText;

namespace AvroSourceGenerator.Diagnostics;

internal readonly record struct LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public static readonly LocationInfo None = default;

    public static LocationInfo FromSourceText(SourceText sourceText)
    {
        var (path, text) = sourceText;
        return new LocationInfo(path, new TextSpan(0, text?.Length ?? 0), new LinePositionSpan(LinePosition.Zero, GetLastLinePosition(text.AsSpan())));

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

    public static LocationInfo FromException(SourceText sourceText, JsonException exception)
    {
        if (sourceText.IsEmpty)
        {
            return FromSourceText(sourceText);
        }

        var csSourceText = Microsoft.CodeAnalysis.Text.SourceText.From(sourceText.Text);
        var lineNumber = exception.LineNumber ?? 0;
        var bytePositionInLine = exception.BytePositionInLine ?? 0;

        var line = csSourceText.Lines[Math.Min((int)lineNumber, csSourceText.Lines.Count - 1)];
        var charIndex = Math.Min((int)bytePositionInLine, line.Span.Length);

        var start = line.Start + charIndex;
        var span = new TextSpan(start, line.End - start);
        var lineSpan = csSourceText.Lines.GetLinePositionSpan(span);

        return new LocationInfo(sourceText.Path, span, lineSpan);
    }

    public static LocationInfo FromSourceSpan(SourceSpan span)
    {
        var textSpan = new TextSpan(span.Offset, span.Length);
        var lineSpan = MapLinePositionSpan(span);
        return new LocationInfo(span.SourceText.Path, textSpan, lineSpan);

        static LinePositionSpan MapLinePositionSpan(SourceSpan span)
        {
            var startIndex = span.SourceText.GetLineIndex(span.Offset);
            var startLine = span.SourceText.Lines[startIndex];
            var startCharacter = span.Offset - startLine.SourceSpan.Offset;
            var startPosition = new LinePosition(startIndex, startCharacter);

            var endIndex = span.SourceText.GetLineIndex(span.Offset + span.Length);
            var endLine = span.SourceText.Lines[endIndex];
            var endCharacter = span.Offset + span.Length - endLine.SourceSpan.Offset;
            var endPosition = new LinePosition(endIndex, endCharacter);

            return new LinePositionSpan(startPosition, endPosition);
        }
    }

    private Location ToLocation() => string.IsNullOrWhiteSpace(FilePath) ? Location.None : Location.Create(FilePath, TextSpan, LineSpan);

    public static implicit operator Location(LocationInfo location) => location.ToLocation();
}
