using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avjs;

public sealed record class JsonObjectSyntax(
    SourceSpan SourceSpan,
    ImmutableArray<JsonPropertySyntax> Properties,
    FrozenDictionary<string, int> NameLookup,
    ImmutableArray<JsonPropertySyntax> Duplicates)
    : JsonSyntax(SourceSpan, JsonTokenType.StartObject)
{
    public JsonPropertySyntax? GetProperty(string propertyName) => NameLookup.TryGetValue(propertyName, out var index) ? Properties[index] : null;

    public ImmutableSortedDictionary<string, JsonElement> GetSchemaProperties() => GetProperties(ReservedSchemaProperties.IsReserved);
    public ImmutableSortedDictionary<string, JsonElement> GetProtocolProperties() => GetProperties(ReservedProtocolProperties.IsReserved);

    private ImmutableSortedDictionary<string, JsonElement> GetProperties(Func<string, bool> isReserved)
    {
        ImmutableSortedDictionary<string, JsonElement>.Builder? properties = null;
        foreach (var (name, value) in Properties)
        {
            if (isReserved(name.Value)) continue;
            properties ??= ImmutableSortedDictionary.CreateBuilder<string, JsonElement>();
            properties[name.Value] = value.AsJsonElement();
        }
        return properties?.ToImmutable() ?? ImmutableSortedDictionary<string, JsonElement>.Empty;
    }
}
