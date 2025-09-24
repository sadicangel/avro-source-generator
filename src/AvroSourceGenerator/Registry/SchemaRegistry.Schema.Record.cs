using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private RecordSchema Record(JsonElement schema, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement>? properties = null)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var fields = Fields(schema, schemaName);

        var recordSchema = new RecordSchema(schema, schemaName, documentation, aliases, fields, properties ?? ImmutableSortedDictionary<string, JsonElement>.Empty);

        _schemas[schemaName] = recordSchema;

        return recordSchema;
    }
}
