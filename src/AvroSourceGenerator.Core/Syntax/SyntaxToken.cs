using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Syntax;

internal sealed record class SyntaxToken(SyntaxKind SyntaxKind, SourceSpan SourceSpan, string? ValueText = null) : ISyntaxNode
{
    public string ValueText { get => field ?? SourceSpan.ToString(); init; } = ValueText;

    public IEnumerable<ISyntaxNode> Children() => [];
}
