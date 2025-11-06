using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private MapSchema Map(
        JsonElement schema,
        string? containingNamespace,
        ImmutableSortedDictionary<string, JsonElement>? properties = null)
    {
        var valuesSchema = schema.GetRequiredProperty("values");

        var values = Schema(valuesSchema, containingNamespace);

        return new MapSchema(values, properties ?? ImmutableSortedDictionary<string, JsonElement>.Empty);
    }
}
