using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public sealed record class EnumDeclarationSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken EnumKeyword,
    SimpleNameSyntax Name,
    SyntaxToken BraceOpenToken,
    SeparatedSyntaxList<SimpleNameSyntax> Symbols,
    SyntaxToken BraceCloseToken)
    : SchemaDeclarationSyntax(SyntaxKind.EnumDeclaration, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return EnumKeyword;
        yield return Name;
        yield return BraceOpenToken;
        foreach (var symbol in Symbols)
        {
            yield return symbol;
        }
        yield return BraceCloseToken;
    }
}
