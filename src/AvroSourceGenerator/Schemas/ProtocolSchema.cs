using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class ProtocolSchema(
    JsonElement Json,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<NamedSchema> Types,
    ImmutableArray<ProtocolMessage> Messages,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : TopLevelSchema(SchemaType.Protocol, Json, SchemaName, Documentation, Properties)
{
    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        writer.WriteString("protocol", SchemaName.Name);
        if (SchemaName.Namespace is not null)
        {
            writer.WriteString("namespace", SchemaName.Namespace);
        }

        if (Types.Length > 0)
        {
            writer.WriteStartArray("types");
            foreach (var type in Types)
            {
                type.WriteTo(writer, registeredSchemas, writtenSchemas, SchemaName.Namespace);
            }

            writer.WriteEndArray();
        }

        if (Messages.Length > 0)
        {
            writer.WriteStartObject("messages");
            foreach (var message in Messages)
            {
                writer.WritePropertyName(message.MethodName is ['@', ..] ? message.MethodName[1..] : message.MethodName);
                message.WriteTo(writer, registeredSchemas, writtenSchemas, SchemaName.Namespace);
            }

            writer.WriteEndObject();
        }

        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
