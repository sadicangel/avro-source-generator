namespace AvroSourceGenerator.Avdl.Syntax;

public readonly record struct SourceSpan(SourceText SourceText, int Offset, int Length)
{
    public ReadOnlySpan<char> Text => SourceText.Text.AsSpan(Offset, Length);
    public override string ToString() => Text.ToString();
}

public sealed record class SyntaxToken(SyntaxKind SyntaxKind, SourceSpan SourceSpan, object? Value = null) : ISyntaxNode
{
    public IEnumerable<ISyntaxNode> Children() => [];
}
