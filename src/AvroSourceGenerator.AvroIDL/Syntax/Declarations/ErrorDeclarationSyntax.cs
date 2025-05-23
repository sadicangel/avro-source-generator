using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public sealed record class ErrorDeclarationSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken ErrorKeyword,
    SimpleNameSyntax Name,
    SyntaxToken BraceOpenToken,
    SyntaxList<FieldDeclarationSyntax> Fields,
    SyntaxToken BraceCloseToken)
    : SchemaDeclarationSyntax(SyntaxKind.ErrorDeclaration, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return ErrorKeyword;
        yield return Name;
        yield return BraceOpenToken;
        foreach (var field in Fields)
        {
            yield return field;
        }
        yield return BraceCloseToken;
    }
}
