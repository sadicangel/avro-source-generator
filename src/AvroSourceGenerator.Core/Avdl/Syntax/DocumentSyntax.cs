using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Directives;

namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class DocumentSyntax(
    SyntaxList<IDirectiveSyntax> Directives,
    SyntaxList<ITopLevelDeclarationSyntax> Declarations
) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.Document;

    public NamespaceDirectiveSyntax? NamespaceDirective => Directives.OfType<NamespaceDirectiveSyntax>().SingleOrDefault();

    public SchemaDirectiveSyntax? SchemaDirective => Directives.OfType<SchemaDirectiveSyntax>().SingleOrDefault();

    public IEnumerable<ImportDirectiveSyntax> ImportDirectives => Directives.OfType<ImportDirectiveSyntax>();

    public IEnumerable<ISyntaxNode> Children()
    {
        foreach (var directive in Directives)
        {
            yield return directive;
        }

        foreach (var declaration in Declarations)
        {
            yield return declaration;
        }
    }
}
