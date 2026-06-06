using System.Text.Json.Nodes;

namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class JsonValueSyntax(SyntaxList<SyntaxToken> SyntaxTokens, JsonNode? Json) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.JsonValue;

    public IEnumerable<ISyntaxNode> Children() => SyntaxTokens;
}
