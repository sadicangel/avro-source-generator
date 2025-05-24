using AvroSourceGenerator.AvroIDL.Syntax.Types;

namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public sealed record class ThrowsErrorClauseSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken ThrowsKeyword,
    SeparatedSyntaxList<NamedTypeSyntax> Errors)
    : SyntaxNode(SyntaxKind.ThrowsErrorClause, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return ThrowsKeyword;
        foreach (var error in Errors)
        {
            yield return error;
        }
    }
}
