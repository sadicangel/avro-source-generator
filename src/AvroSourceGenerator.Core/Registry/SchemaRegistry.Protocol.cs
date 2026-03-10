using System.Text.Json;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private ProtocolSchema Protocol(JsonElement schema, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredProtocolName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        using (EnterRecursionScope(schemaName))
        {
            var documentation = schema.GetDocumentation();
            var types = ProtocolTypes(schema.GetRequiredArray(AvroJsonKeys.Types), schemaName.Namespace);
            var messages = ProtocolMessages(schema.GetRequiredObject(AvroJsonKeys.Messages), schemaName.Namespace);
            var properties = schema.GetProtocolProperties();

            var protocolSchema = new ProtocolSchema(schema, schemaName, documentation, types, messages, properties);
            _schemas[schemaName] = protocolSchema;

            return protocolSchema;
        }
    }
}
