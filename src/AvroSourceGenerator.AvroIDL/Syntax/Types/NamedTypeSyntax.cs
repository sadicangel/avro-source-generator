
using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Syntax.Types;

public sealed record class NamedTypeSyntax(SyntaxTree SyntaxTree, NameSyntax Name)
    : TypeSyntax(SyntaxKind.NamedType, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return Name;
    }
}
