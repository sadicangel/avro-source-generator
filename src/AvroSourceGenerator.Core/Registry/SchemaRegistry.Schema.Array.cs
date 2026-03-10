using System.Text.Json;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private ArraySchema Array(JsonElement schema, string? containingNamespace)
    {
        var itemsSchema = schema.GetRequiredProperty(AvroJsonKeys.Items);

        var items = Schema(itemsSchema, containingNamespace);
        var documentation = schema.GetDocumentation();
        var properties = schema.GetSchemaProperties();

        return new ArraySchema(items, documentation, properties);
    }
}
