using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private ArraySchema Array(JsonElement schema, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var itemsSchema = schema.GetRequiredProperty(AvroJsonKeys.Items);

        var items = Schema(itemsSchema, containingNamespace);

        return new ArraySchema(items, properties);
    }
}
