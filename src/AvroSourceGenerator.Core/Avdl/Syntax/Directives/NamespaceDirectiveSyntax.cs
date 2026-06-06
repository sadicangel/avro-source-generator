namespace AvroSourceGenerator.Avdl.Syntax.Directives;

public sealed record class NamespaceDirectiveSyntax(
    SyntaxToken NamespaceKeyword,
    INameSyntax NamespaceName,
    SyntaxToken SemicolonToken)
    : IDirectiveSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.NamespaceDirective;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return NamespaceKeyword;
        yield return NamespaceName;
        yield return SemicolonToken;
    }
}
