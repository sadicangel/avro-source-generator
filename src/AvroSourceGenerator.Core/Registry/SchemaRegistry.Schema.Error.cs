using System.Text.Json;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private ErrorSchema Error(JsonElement schema, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);
        using (EnterRecursionScope(schemaName))
        {
            var documentation = schema.GetDocumentation();
            var aliases = schema.GetAliases();
            var fields = Fields(schema, schemaName);
            var properties = schema.GetSchemaProperties();

            var errorSchema = new ErrorSchema(schema, schemaName, documentation, aliases, fields, properties);

            Register(errorSchema);

            return errorSchema;
        }
    }
}
