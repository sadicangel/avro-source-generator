using System.Text.Json;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private RecordSchema Record(JsonElement schema, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        using (EnterRecursionScope(schemaName))
        {
            var documentation = schema.GetDocumentation();
            var aliases = schema.GetAliases();
            var fields = Fields(schema, schemaName);
            var properties = schema.GetSchemaProperties();

            var recordSchema = new RecordSchema(schema, schemaName, documentation, aliases, fields, properties);

            _schemas[schemaName] = recordSchema;

            return recordSchema;
        }
    }
}
