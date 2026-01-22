using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class ProtocolRequestParameter(
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
        writer.WriteString("name", Name is ['@', ..] ? Name[1..] : Name);
        writer.WritePropertyName("type");
        Type.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        if (Documentation is not null)
        {
            writer.WriteString("doc", Documentation);
        }

        if (DefaultJson is not null)
        {
            writer.WritePropertyName("default");
            DefaultJson.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
