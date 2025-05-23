using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Syntax;

public sealed record class SyntaxTrivia(
    SyntaxKind SyntaxKind,
    SyntaxTree SyntaxTree,
    SourceSpan SourceSpan)
    : SyntaxNode(SyntaxKind, SyntaxTree)
{
    public override SourceSpan SourceSpan { get; } = SourceSpan;

    public override SourceSpan SourceSpanWithTrivia { get => SourceSpan; }

    public override IEnumerable<SyntaxNode> Children() => [];
}
