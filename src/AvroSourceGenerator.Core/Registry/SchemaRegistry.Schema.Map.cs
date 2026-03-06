using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private MapSchema Map(
        JsonElement schema,
        string? containingNamespace,
        ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var valuesSchema = schema.GetRequiredProperty(AvroJsonKeys.Values);

        var values = Schema(valuesSchema, containingNamespace);

        return new MapSchema(values, properties);
    }
}
