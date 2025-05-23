namespace AvroSourceGenerator.AvroIDL.Text;

public sealed record class SourceLine(SourceText Source, SourceSpan SourceSpan, SourceSpan SourceSpanWithLineBreak)
{
    public override string ToString() => SourceSpan.ToString();
}
