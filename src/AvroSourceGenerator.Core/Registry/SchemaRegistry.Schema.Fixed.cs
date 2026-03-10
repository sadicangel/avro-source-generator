using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private FixedSchema Fixed(JsonElement schema, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        using (EnterRecursionScope(schemaName))
        {
            var documentation = schema.GetDocumentation();
            var aliases = schema.GetAliases();
            var size = schema.GetFixedSize();
            var properties = schema.GetSchemaProperties();

            var fixedSchema = targetProfile switch
            {
                // Only Apache.Avro needs a custom type for fixed, others use byte[].
                TargetProfile.Apache => new FixedSchema(schema, schemaName, documentation, aliases, size, properties),
                _ => FixedSchema.CreateAsByteArray(schema, schemaName, documentation, aliases, size, properties),
            };

            _schemas[schemaName] = fixedSchema;

            return fixedSchema;
        }
    }
}
