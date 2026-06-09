using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Directives;

namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class DocumentSyntax(
    SyntaxList<IDirectiveSyntax> Directives,
    SyntaxList<IDeclarationSyntax> Declarations
) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.Document;

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
