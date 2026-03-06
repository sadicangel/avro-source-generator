using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class ProtocolRequestParameter(
    string Name,
    AvroSchema Type,
    AvroSchema UnderlyingType,
    bool IsNullable,
    string? Documentation,
    JsonElement? DefaultJson,
    object? Default)
{
    public void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        writer.WriteString(AvroJsonKeys.Name, Name is ['@', ..] ? Name[1..] : Name);
        writer.WritePropertyName(AvroJsonKeys.Type);
        Type.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        if (Documentation is not null)
        {
            writer.WriteString(AvroJsonKeys.Doc, Documentation);
        }

        if (DefaultJson is not null)
        {
            writer.WritePropertyName(AvroJsonKeys.Default);
            DefaultJson.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
