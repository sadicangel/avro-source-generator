using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Protocols;

public sealed record class ProtocolResponse(
    AvroSchema Type,
    AvroSchema UnderlyingType,
    bool IsNullable)
{
    public void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace) =>
        Type.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
}

