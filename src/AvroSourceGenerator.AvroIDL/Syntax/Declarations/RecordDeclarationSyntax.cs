using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public sealed record class RecordDeclarationSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken RecordKeyword,
    SimpleNameSyntax Name,
    SyntaxToken BraceOpenToken,
    SyntaxList<FieldDeclarationSyntax> Fields,
    SyntaxToken BraceCloseToken)
    : SchemaDeclarationSyntax(SyntaxKind.RecordDeclaration, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return RecordKeyword;
        yield return Name;
        yield return BraceOpenToken;
        foreach (var field in Fields)
        {
            yield return field;
        }
        yield return BraceCloseToken;
    }
}
