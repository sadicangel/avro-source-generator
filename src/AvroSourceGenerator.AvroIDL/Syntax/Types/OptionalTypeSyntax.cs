
namespace AvroSourceGenerator.AvroIDL.Syntax.Types;

public sealed record class OptionalTypeSyntax(
    SyntaxTree SyntaxTree,
    TypeSyntax Type,
    SyntaxToken HookToken)
    : TypeSyntax(SyntaxKind.OptionalType, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return Type;
        yield return HookToken;
    }
}
