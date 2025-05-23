using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Syntax;

public sealed record class SyntaxToken(
    SyntaxKind SyntaxKind,
    SyntaxTree SyntaxTree,
    SourceSpan SourceSpan,
    SyntaxList<SyntaxTrivia> LeadingTrivia,
    SyntaxList<SyntaxTrivia> TrailingTrivia,
    object? Value)
    : SyntaxNode(SyntaxKind, SyntaxTree)
{
    public bool IsSynthetic { get => SourceSpan.Length == 0; }

    public override SourceSpan SourceSpan { get; } = SourceSpan;

    public override SourceSpan SourceSpanWithTrivia => SourceSpan.Combine(
        LeadingTrivia.FirstOrDefault()?.SourceSpanWithTrivia ?? SourceSpan,
        TrailingTrivia.LastOrDefault()?.SourceSpanWithTrivia ?? SourceSpan);

    public override IEnumerable<SyntaxNode> Children() => [];

    public static SyntaxToken CreateSynthetic(SyntaxKind syntaxKind, SyntaxTree syntaxTree, int offset = -1) => new(
        SyntaxKind: syntaxKind,
        SyntaxTree: syntaxTree,
        SourceSpan: new SourceSpan(syntaxTree.SourceText, offset >= 0 ? offset : syntaxTree.SourceText.Text.Length, 0),
        LeadingTrivia: [],
        TrailingTrivia: [],
        Value: null);
}
