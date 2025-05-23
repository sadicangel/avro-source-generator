using System.Diagnostics;

namespace AvroSourceGenerator.AvroIDL.Text;

public readonly record struct SourceSpan(SourceText SourceText, int Offset, int Length)
{
    public readonly ReadOnlySpan<char> Text => SourceText.Text.AsSpan(Offset, Length);

    public string FileName { get => SourceText.FileName; }
    public string FilePath { get => SourceText.FilePath; }
    public int StartLine => SourceText.GetLineIndex(Offset);
    public int StartCharacter => Offset - SourceText.Lines[StartLine].SourceSpan.Offset;
    public int EndLine => SourceText.GetLineIndex(Offset + Length);
    public int EndCharacter => Offset + Length - SourceText.Lines[StartLine].SourceSpan.Offset;

    public override string ToString() => Text.ToString();

    public static SourceSpan Combine(SourceSpan left, SourceSpan right)
    {
        Debug.Assert(left.SourceText == right.SourceText, "Cannot combine spans from different source texts.");
        return new SourceSpan(left.SourceText, left.Offset, right.Offset + right.Length - left.Offset);
    }
}
