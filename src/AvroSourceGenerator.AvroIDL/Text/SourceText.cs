namespace AvroSourceGenerator.AvroIDL.Text;

public sealed record class SourceText(string Text, string FilePath)
{
    public SourceText(string text) : this(text, string.Empty) { }

    public string FileName { get; } = string.IsNullOrEmpty(FilePath) ? string.Empty : Path.GetFileName(FilePath);

    public int Length { get => Text.Length; }

    public ReadOnlyList<SourceLine> Lines { get => field ??= ParseLines(Text); }

    internal char this[Index index]
    {
        get
        {
            var offset = index.GetOffset(Length);
            if (offset < 0 || offset >= Length)
                return '\0';
            return Text[offset];
        }
    }

    public int GetLineIndex(int offset)
    {
        var lower = 0;
        var upper = Lines.Count - 1;

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

    public override string ToString() => Text;

    private ReadOnlyList<SourceLine> ParseLines(ReadOnlySpan<char> text)
    {
        var lines = new List<SourceLine>();

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
                lines.Add(new SourceLine(
                    this,
                    new SourceSpan(this, lineStart, position - lineStart),
                    new SourceSpan(this, lineStart, position - lineStart + lineBreakWidth)));
                position += lineBreakWidth;
                lineStart = position;
            }
        }

        if (position >= lineStart)
            lines.Add(new SourceLine(this, new SourceSpan(this, lineStart, position - lineStart), new SourceSpan(this, lineStart, position - lineStart)));

        return new(lines);
    }
}
