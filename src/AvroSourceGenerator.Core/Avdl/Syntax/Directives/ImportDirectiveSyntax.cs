namespace AvroSourceGenerator.Avdl.Syntax.Directives;

public sealed record class ImportDirectiveSyntax(
    SyntaxToken ImportKeyword,
    SyntaxToken ImportTypeKeyword,
    SyntaxToken ImportPathLiteralToken,
    SyntaxToken SemicolonToken)
    : IDirectiveSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.ImportDirective;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return ImportKeyword;
        yield return ImportTypeKeyword;
        yield return ImportPathLiteralToken;
        yield return SemicolonToken;
    }
}
