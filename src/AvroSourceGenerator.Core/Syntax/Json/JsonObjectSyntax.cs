using System.Collections.Frozen;
using System.Collections.Immutable;

namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonObjectSyntax : JsonValueSyntax
{
    private readonly FrozenDictionary<string, int> _nameLookup;

    public SyntaxToken BraceOpenToken { get; }
    public ImmutableArray<JsonPropertySyntax> Members { get; }
    public ImmutableArray<JsonPropertySyntax> Properties { get; }
    public SyntaxToken BraceCloseToken { get; }
    public ImmutableArray<JsonPropertySyntax> Duplicates { get; }
    public override SyntaxKind SyntaxKind => SyntaxKind.JsonObject;

    public JsonObjectSyntax(SyntaxToken open, ImmutableArray<JsonPropertySyntax> members, SyntaxToken close)
        : base(JsonSyntaxKind.Object)
    {
        BraceOpenToken = open;
        Members = members;
        BraceCloseToken = close;

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

        Properties = duplicates.Count == 0 ? members : properties.DrainToImmutable();
        Duplicates = duplicates.DrainToImmutable();
        _nameLookup = lookup.ToFrozenDictionary();
    }

    public JsonPropertySyntax? Property(string propertyName) => _nameLookup.TryGetValue(propertyName, out var index) ? Properties[index] : null;

    public override IEnumerable<ISyntaxNode> Children()
    {
        yield return BraceOpenToken;
        foreach (var member in Members) yield return member;
        yield return BraceCloseToken;
    }
}
