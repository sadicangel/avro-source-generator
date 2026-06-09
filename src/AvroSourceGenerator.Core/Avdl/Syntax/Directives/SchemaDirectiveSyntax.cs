using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Syntax.Directives;

public sealed record class SchemaDirectiveSyntax(
    SyntaxToken SchemaKeyword,
    ITypeSyntax MainSchemaType,
    SyntaxToken SemicolonToken)
    : IDirectiveSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.SchemaDirective;

    public SimpleNameSyntax? MainSchemaName => (MainSchemaType as NamedTypeSyntax)?.Name as SimpleNameSyntax;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return SchemaKeyword;
        yield return MainSchemaType;
        yield return SemicolonToken;
    }
}
