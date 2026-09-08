namespace AvroSourceGenerator.Text;

public readonly record struct SourceLine(SourceSpan SourceSpan, SourceSpan SourceSpanWithLineBreak)
{
    public SourceText SourceText => SourceSpan.SourceText;

    public int Length => SourceSpan.Length;

    public override string ToString() => SourceSpan.ToString();
}
