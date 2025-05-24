
namespace AvroSourceGenerator.AvroIDL.Syntax;
public sealed record class ImportSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken ImportKeyword,
    SyntaxToken ImportTypeKeyword,
    SyntaxToken FileNameLiteralToken,
    SyntaxToken SemicolonToken)
    : SyntaxNode(SyntaxKind.Import, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return ImportKeyword;
        yield return ImportTypeKeyword;
        yield return FileNameLiteralToken;
        yield return SemicolonToken;
    }
}
