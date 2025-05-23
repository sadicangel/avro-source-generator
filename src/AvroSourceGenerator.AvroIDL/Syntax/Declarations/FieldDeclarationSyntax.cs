using AvroSourceGenerator.AvroIDL.Syntax.Names;
using AvroSourceGenerator.AvroIDL.Syntax.Types;

namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public sealed record class FieldDeclarationSyntax(
    SyntaxTree SyntaxTree,
    TypeSyntax Type,
    SimpleNameSyntax Name,
    DefaultValueClauseSyntax? DefaultValueClause,
    SyntaxToken SemicolonToken)
    : SyntaxNode(SyntaxKind.FieldDeclaration, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return Type;
        yield return Name;
        if (DefaultValueClause is not null)
        {
            yield return DefaultValueClause;
        }
        yield return SemicolonToken;
    }
}
