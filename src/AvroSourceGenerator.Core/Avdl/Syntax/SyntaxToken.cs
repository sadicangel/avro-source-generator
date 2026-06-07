namespace AvroSourceGenerator.Avdl.Syntax;

public readonly record struct SourceSpan(SourceText SourceText, int Offset, int Length)
{
    public override string ToString() => SourceText.Text.AsSpan(Offset, Length).ToString();
}

public sealed record class SyntaxToken(SyntaxKind SyntaxKind, SourceSpan SourceSpan, object? Value = null) : ISyntaxNode
{
    public string ValueText => Value?.ToString() ?? SourceSpan.ToString();

    public IEnumerable<ISyntaxNode> Children() => [];
}
