using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private ProtocolSchema Protocol(JsonElement schema, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement>? properties = null)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace, propertyName: "protocol");

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var types = ProtocolTypes(schema.GetRequiredArray("types"), schemaName.Namespace);
        var messages = ProtocolMessages(schema.GetRequiredObject("messages"), schemaName.Namespace);

        var protocolSchema = new ProtocolSchema(schema, schemaName, documentation, types, messages, properties ?? ImmutableSortedDictionary<string, JsonElement>.Empty);
        _schemas[schemaName] = protocolSchema;

        return protocolSchema;
    }
}
