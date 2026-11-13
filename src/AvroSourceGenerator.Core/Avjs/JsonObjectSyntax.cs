using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avjs;

internal sealed record class JsonObjectSyntax(
    SourceSpan SourceSpan,
    ImmutableArray<JsonPropertySyntax> Properties,
    FrozenDictionary<string, int> NameLookup,
    ImmutableArray<JsonPropertySyntax> Duplicates)
    : JsonSyntax(SourceSpan, JsonTokenType.StartObject)
{
    public JsonPropertySyntax? Property(string propertyName) => NameLookup.TryGetValue(propertyName, out var index) ? Properties[index] : null;
}
