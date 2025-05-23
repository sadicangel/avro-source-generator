using AvroSourceGenerator.AvroIDL.Syntax.Declarations;
using AvroSourceGenerator.AvroIDL.Syntax.Names;

namespace AvroSourceGenerator.AvroIDL.Syntax;
public sealed record class AnnotationSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken AtSignToken,
    SimpleNameSyntax Name,
    SyntaxToken ParenthesisOpenToken,
    JsonValueSyntax Json,
    SyntaxToken ParenthesisCloseToken)
    : SyntaxNode(SyntaxKind.Annotation, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return AtSignToken;
        yield return Name;
        yield return ParenthesisOpenToken;
        yield return Json;
        yield return ParenthesisCloseToken;
    }
}
