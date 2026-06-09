namespace AvroSourceGenerator.Avdl.Text;

public readonly record struct SourceSpan(SourceText SourceText, int Offset, int Length)
{
    public override string ToString() => SourceText.Text.AsSpan(Offset, Length).ToString();
}
