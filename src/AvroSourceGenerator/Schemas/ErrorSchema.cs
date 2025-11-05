using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class ErrorSchema(
    JsonElement Json,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<Field> Fields,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : NamedSchema(SchemaType.Error, Json, SchemaName, Documentation, Aliases, Properties)
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        if (!writtenSchemas.Add(SchemaName))
        {
            var name = SchemaName.Namespace is null || SchemaName.Namespace == containingNamespace ? SchemaName.Name : $"{SchemaName.Namespace}.{SchemaName.Name}";
            writer.WriteStringValue(name);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("type", "error");
        writer.WriteString("name", SchemaName.Name);
        var @namespace = SchemaName.Namespace ?? containingNamespace;
        if (@namespace is not null)
            writer.WriteString("namespace", @namespace);
        if (Documentation is not null)
            writer.WriteString("doc", Documentation);
        if (Aliases.Length > 0)
        {
            writer.WriteStartArray("aliases");
            foreach (var alias in Aliases)
                writer.WriteStringValue(alias);
            writer.WriteEndArray();
        }

        writer.WriteStartArray("fields");
        foreach (var field in Fields)
            field.WriteTo(writer, writtenSchemas, @namespace);
        writer.WriteEndArray();
        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
