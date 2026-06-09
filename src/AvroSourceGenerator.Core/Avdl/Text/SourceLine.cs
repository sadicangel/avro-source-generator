namespace AvroSourceGenerator.Avdl.Text;

public readonly record struct SourceLine(SourceSpan SourceSpan, SourceSpan SourceSpanWithLineBreak)
{
    public SourceText SourceText => SourceSpan.SourceText;

    public override string ToString() => SourceSpan.ToString();
}
