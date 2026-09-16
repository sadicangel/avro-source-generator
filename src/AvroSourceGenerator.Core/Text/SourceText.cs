using System.Collections.Immutable;

namespace AvroSourceGenerator.Text;

public sealed class SourceText : IEquatable<SourceText>
{
    private readonly Lazy<ImmutableArray<SourceLine>> _lines;

    public SourceText(string path, string text)
    {
        Path = new SourcePath(path);
        Text = text;
        _lines = new Lazy<ImmutableArray<SourceLine>>(() => ParseLines(this));
    }

    public SourcePath Path { get; }

    public string Text { get; }

    public bool IsEmpty => Path.IsEmpty || string.IsNullOrWhiteSpace(Text);

    public int Length => Text.Length;

    public ImmutableArray<SourceLine> Lines => _lines.Value;

    public SourceSpan GetSpan(int offset, int length) => new SourceSpan(this, offset, length);

    public int GetOffset(int lineIndex, int columnIndex)
    {
        if (lineIndex < 0 || lineIndex >= Lines.Length)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));
        var line = Lines[lineIndex];
        if (columnIndex < 0 || columnIndex > line.Length)
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        return line.SourceSpan.Offset + columnIndex;
    }

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

    public bool Equals(SourceText? other) => other is not null && Path == other.Path && Text == other.Text;

    public override bool Equals(object? obj) => obj is SourceText other && Equals(other);

    public static bool operator ==(SourceText? left, SourceText? right) => Equals(left, right);

    public static bool operator !=(SourceText? left, SourceText? right) => !Equals(left, right);

    public override int GetHashCode() => HashCode.Combine(Path, Text);

    private static ImmutableArray<SourceLine> ParseLines(SourceText sourceText)
    {
        var text = sourceText.Text;
        var lines = ImmutableArray.CreateBuilder<SourceLine>();

        var position = 0;
        var lineStart = 0;
        while (position < text.Length)
        {
            var lineBreakWidth = text.AsSpan(position) switch
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

        return lines.DrainToImmutable();
    }
}
