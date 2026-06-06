namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class QualifiedNameSyntax(SeparatedSyntaxList<SyntaxToken> Identifiers) : INameSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.QualifiedName;

    public string FullName => field ??= string.Concat(Identifiers.SyntaxNodes.Select(n => (n as SyntaxToken)?.SourceSpan.ToString() ?? SyntaxFacts.GetText(n.SyntaxKind)));

    public IEnumerable<ISyntaxNode> Children() => Identifiers.SyntaxNodes;
}
