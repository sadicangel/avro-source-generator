using System.Collections.Frozen;
using System.Collections.Immutable;

namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonObjectSyntax(
    SyntaxToken BraceOpenToken,
    ImmutableArray<JsonPropertySyntax> Properties,
    SyntaxToken BraceCloseToken,
    FrozenDictionary<string, int> NameLookup,
    ImmutableArray<JsonPropertySyntax> Duplicates)
    : JsonValueSyntax(JsonSyntaxKind.Object)
{
    public override SyntaxKind SyntaxKind => SyntaxKind.JsonObject;

    public static JsonObjectSyntax Create(SyntaxToken open, ImmutableArray<JsonPropertySyntax> members, SyntaxToken close)
    {
        var properties = ImmutableArray.CreateBuilder<JsonPropertySyntax>();
        var lookup = new Dictionary<string, int>(StringComparer.Ordinal);
        var duplicates = ImmutableArray.CreateBuilder<JsonPropertySyntax>();
        foreach (var member in members)
        {
            if (lookup.TryGetValue(member.Name.ValueText, out var index))
            {
                duplicates.Add(properties[index]);
                properties[index] = member;
            }
            else
            {
                lookup.Add(member.Name.ValueText, properties.Count);
                properties.Add(member);
            }
        }
        var obj = new JsonObjectSyntax(open, properties.DrainToImmutable(), close, lookup.ToFrozenDictionary(), duplicates.DrainToImmutable());
        foreach (var member in members)
        {
            member.Parent = obj;
            member.Value.Parent = member;
        }
        return obj;
    }

    public JsonPropertySyntax? Property(string propertyName) => NameLookup.TryGetValue(propertyName, out var index) ? Properties[index] : null;

    public override IEnumerable<ISyntaxNode> Children()
    {
        yield return BraceOpenToken;
        foreach (var child in Properties) yield return child;
        yield return BraceCloseToken;
    }
}
