namespace AvroSourceGenerator.Avdl.Syntax.Directives;

public sealed record class SchemaDirectiveSyntax(
    SyntaxToken SchemaKeyword,
    SimpleNameSyntax MainSchemaName,
    SyntaxToken SemicolonToken)
    : IDirectiveSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.SchemaDirective;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return SchemaKeyword;
        yield return MainSchemaName;
        yield return SemicolonToken;
    }
}
