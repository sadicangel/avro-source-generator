using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class Field(
    string Name,
    AvroSchema Type,
    AvroSchema UnderlyingType,
    bool IsNullable,
    string? Documentation,
    ImmutableArray<string> Aliases,
    JsonElement? DefaultJson,
    object? Default,
    int? Order,
    ImmutableSortedDictionary<string, JsonElement> Properties,
    string? Remarks)
{
    public void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        // TODO: Is it worth to store the schema name?
        writer.WriteString("name", Name is ['@', ..] ? Name[1..] : Name);
        writer.WritePropertyName("type");
        Type.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        if (Documentation is not null)
            writer.WriteString("doc", Documentation);
        if (Aliases.Length > 0)
        {
            writer.WriteStartArray("aliases");
            foreach (var alias in Aliases)
                writer.WriteStringValue(alias);
            writer.WriteEndArray();
        }

        if (DefaultJson is not null)
        {
            writer.WritePropertyName("default");
            DefaultJson.Value.WriteTo(writer);
        }

        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
