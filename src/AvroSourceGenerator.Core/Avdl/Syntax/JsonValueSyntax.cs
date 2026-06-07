using System.Text.Json.Nodes;

namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class JsonValueSyntax(SyntaxList<SyntaxToken> SyntaxTokens, JsonNode? JsonNode) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.JsonValue;

    public IEnumerable<ISyntaxNode> Children() => SyntaxTokens;
}
