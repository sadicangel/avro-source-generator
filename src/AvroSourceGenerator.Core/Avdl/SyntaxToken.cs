using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avdl;

internal sealed record class SyntaxToken(SyntaxKind SyntaxKind, SourceSpan SourceSpan, object? Value = null) : ISyntaxNode
{
    public string ValueText => Value?.ToString() ?? SourceSpan.ToString();

    public IEnumerable<ISyntaxNode> Children() => [];
}
