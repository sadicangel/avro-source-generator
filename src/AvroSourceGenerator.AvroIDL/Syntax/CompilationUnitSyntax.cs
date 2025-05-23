namespace AvroSourceGenerator.AvroIDL.Syntax;

public sealed record class CompilationUnitSyntax(
    SyntaxTree SyntaxTree,
    SyntaxList<SyntaxNode> SyntaxNodes,
    SyntaxToken EofToken
    ) : SyntaxNode(SyntaxKind.CompilationUnit, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children() => SyntaxNodes;
}
