using System.Text.Json;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private MapSchema Map(JsonElement schema, string? containingNamespace)
    {
        var valuesSchema = schema.GetRequiredProperty(AvroJsonKeys.Values);

        var values = Schema(valuesSchema, containingNamespace);
        var documentation = schema.GetDocumentation();
        var properties = schema.GetSchemaProperties();

        return new MapSchema(values, documentation, properties);
    }
}
