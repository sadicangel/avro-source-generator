using System.Collections.Immutable;

namespace AvroSourceGenerator.Avdl.Text;

public readonly record struct SourceText(string Path, string Text)
{
    private readonly Lazy<ImmutableArray<SourceLine>> _lines = new(() => ParseLines(Text, Path));

    public ImmutableArray<SourceLine> Lines => _lines.Value;

    public SourceSpan GetSpan(int offset, int length) => new(this, offset, length);

    public int GetLineIndex(int offset)
    {
        var lower = 0;
        var upper = Lines.Length - 1;

        while (lower <= upper)
        {
            var index = lower + (upper - lower) / 2;
            var start = Lines[index].SourceSpan.Offset;

            if (offset == start)
                return index;

            if (start > offset)
                upper = index - 1;
            else
                lower = index + 1;
        }

        return lower - 1;
    }

    private static ImmutableArray<SourceLine> ParseLines(string text, string path)
    {
        var sourceText = new SourceText(path, text);
        var lines = ImmutableArray.CreateBuilder<SourceLine>();

        var position = 0;
        var lineStart = 0;
        while (position < text.Length)
        {
            var lineBreakWidth = text[position..] switch
            {
                ['\r', '\n', ..] => 2,
                ['\r', ..] or ['\n', ..] => 1,
                _ => 0,
            };

            if (lineBreakWidth == 0)
            {
                position++;
            }
            else
            {
                lines.Add(new SourceLine(sourceText.GetSpan(lineStart, position - lineStart), sourceText.GetSpan(lineStart, position - lineStart + lineBreakWidth)));
                position += lineBreakWidth;
                lineStart = position;
            }
        }

        if (position >= lineStart)
            lines.Add(new SourceLine(sourceText.GetSpan(lineStart, position - lineStart), sourceText.GetSpan(lineStart, position - lineStart)));

        return lines.ToImmutable();
    }
}
