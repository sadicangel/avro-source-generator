using System.Collections.Immutable;

namespace AvroSourceGenerator.Text;

public sealed record class SourceText(string Path, string Text) : IEquatable<SourceText>
{
    // TODO: Replace literals with constants for file extensions and do a project wide replace.
    public SourceType Type =>
        Path.EndsWith(".avsc", StringComparison.OrdinalIgnoreCase) ? SourceType.Avsc :
        Path.EndsWith(".avpr", StringComparison.OrdinalIgnoreCase) ? SourceType.Avpr :
        Path.EndsWith(".avdl", StringComparison.OrdinalIgnoreCase) ? SourceType.Avdl :
        throw new InvalidOperationException("Unreachable: Unsupported Avro file type.");

    public bool IsEmpty => string.IsNullOrWhiteSpace(Text);

    public int Length => Text.Length;

    public ImmutableArray<SourceLine> Lines
    {
        get
        {
            if (field.IsDefault)
                ImmutableInterlocked.InterlockedInitialize(ref field, ParseLines(Text, Path));
            return field;
        }
    }

    public SourceSpan GetSpan(int offset, int length) => new(this, offset, length);

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

    public override int GetHashCode() => HashCode.Combine(Path, Text);

    private static ImmutableArray<SourceLine> ParseLines(string text, string path)
    {
        var sourceText = new SourceText(path, text);
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
