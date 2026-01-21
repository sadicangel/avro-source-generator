using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private FixedSchema Fixed(JsonElement schema, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var size = schema.GetFixedSize();

        var fixedSchema = avroLibrary switch
        {
            // Only Apache.Avro needs a custom type for fixed, others use byte[].
            AvroLibrary.Apache => new FixedSchema(schema, schemaName, documentation, aliases, size, properties),
            _ => FixedSchema.CreateAsByteArray(schema, schemaName, documentation, aliases, size, properties),
        };

        _schemas[schemaName] = fixedSchema;

        return fixedSchema;
    }
}
