using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Parsing;
internal static class LocationExtensions
{
    public static Location GetLocation(this string path, string? text) =>
        Location.Create(path, new TextSpan(0, text?.Length ?? 0), new LinePositionSpan(LinePosition.Zero, text.GetLastLinePosition()));

    public static Location GetLocation(this string path, TextSpan textSpan, LinePositionSpan lineSpan) =>
        Location.Create(path, textSpan, lineSpan);

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
