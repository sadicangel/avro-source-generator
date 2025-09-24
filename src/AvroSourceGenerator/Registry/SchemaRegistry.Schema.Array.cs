using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private ArraySchema Array(JsonElement schema, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement>? properties = null)
    {
        var itemsSchema = schema.GetRequiredProperty("items");

        var items = Schema(itemsSchema, containingNamespace);

        return new ArraySchema(items, properties ?? ImmutableSortedDictionary<string, JsonElement>.Empty);
    }
}
