using System.Collections.Immutable;

namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonArraySyntax(
    SyntaxToken BracketOpenToken,
    ImmutableArray<JsonValueSyntax> Elements,
    SyntaxToken BracketCloseToken) : JsonValueSyntax(JsonSyntaxKind.Array)
{
    public override SyntaxKind SyntaxKind => SyntaxKind.JsonArray;

    public override IEnumerable<ISyntaxNode> Children()
    {
        yield return BracketOpenToken;
        foreach (var child in Elements) yield return child;
        yield return BracketCloseToken;
    }
}
