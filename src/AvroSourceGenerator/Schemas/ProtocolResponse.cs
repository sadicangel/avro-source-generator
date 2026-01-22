using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class ProtocolResponse(
    AvroSchema Type,
    AvroSchema UnderlyingType,
    bool IsNullable)
{
    public void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace) =>
        Type.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
}
