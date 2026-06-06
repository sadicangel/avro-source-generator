using AvroSourceGenerator.Avdl.Syntax.Declarations;
using AvroSourceGenerator.Avdl.Syntax.Directives;

namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class CompilationUnitSyntax(
    SyntaxList<IDirectiveSyntax> Directives,
    SyntaxList<IDeclarationSyntax> Declarations
) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.CompilationUnit;

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
