namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class DocumentationSyntax(SyntaxToken DocumentationTrivia) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.Documentation;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return DocumentationTrivia;
    }
}
