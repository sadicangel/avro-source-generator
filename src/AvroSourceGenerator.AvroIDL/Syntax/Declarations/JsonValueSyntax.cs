using System.Text.Json.Nodes;

namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;
public sealed record class JsonValueSyntax(
    SyntaxTree SyntaxTree,
    SyntaxList<SyntaxToken> SyntaxTokens,
    JsonNode? Json)
    : SyntaxNode(SyntaxKind.JsonValue, SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children() => SyntaxTokens;
}
