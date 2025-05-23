using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;
public sealed record class ProtocolDeclarationSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken ProtocolKeyword,
    SimpleNameSyntax Name,
    SyntaxToken BraceOpenToken,
    SyntaxList<SchemaDeclarationSyntax> Types,
    SyntaxList<MessageDeclarationSyntax> Messages,
    SyntaxToken BraceCloseToken)
    : DeclarationSyntax(SyntaxKind.ProtocolDeclaration, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return ProtocolKeyword;
        yield return Name;
        yield return BraceOpenToken;
        foreach (var type in Types)
        {
            yield return type;
        }
        foreach (var message in Messages)
        {
            yield return message;
        }
        yield return BraceCloseToken;
    }
}
