using System.Collections.Immutable;

namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonArraySyntax(
    SyntaxToken BracketOpenToken,
    ImmutableArray<JsonValueSyntax> Elements,
    SyntaxToken BracketCloseToken) : JsonValueSyntax(JsonSyntaxKind.Array)
{
    public override SyntaxKind SyntaxKind => SyntaxKind.JsonArray;

    public static JsonArraySyntax Create(SyntaxToken open, ImmutableArray<JsonValueSyntax> elements, SyntaxToken close)
    {
        var array = new JsonArraySyntax(open, elements, close);
        foreach (var element in elements) element.Parent = array;
        return array;
    }

    public override IEnumerable<ISyntaxNode> Children()
    {
        yield return BracketOpenToken;
        foreach (var child in Elements) yield return child;
        yield return BracketCloseToken;
    }
}
