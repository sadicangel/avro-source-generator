using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class RecordSchema(
    JsonElement Json,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<Field> Fields,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : NamedSchema(SchemaType.Record, Json, SchemaName, Documentation, Aliases, Properties)
{
    public AvroSchema? InheritsFrom { get; set; }

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        if (!writtenSchemas.Add(SchemaName))
        {
            writer.WriteStringValue(SchemaName.RelativeTo(containingNamespace));
            return;
        }

        writer.WriteStartObject();
        writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Record);
        writer.WriteString(AvroJsonKeys.Name, SchemaName.Name);
        var @namespace = SchemaName.Namespace ?? containingNamespace;
        if (@namespace is not null)
            writer.WriteString(AvroJsonKeys.Namespace, @namespace);
        if (Documentation is not null)
            writer.WriteString(AvroJsonKeys.Doc, Documentation);
        if (Aliases.Length > 0)
        {
            writer.WriteStartArray(AvroJsonKeys.Aliases);
            foreach (var alias in Aliases)
                writer.WriteStringValue(alias);
            writer.WriteEndArray();
        }

        writer.WriteStartArray(AvroJsonKeys.Fields);
        foreach (var field in Fields)
            field.WriteTo(writer, writtenSchemas, registeredSchemas, @namespace);
        writer.WriteEndArray();
        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
