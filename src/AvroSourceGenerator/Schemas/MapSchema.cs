using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class MapSchema(AvroSchema ValueSchema, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType.Map, new CSharpName($"Dictionary<string, {ValueSchema}>", "System.Collections.Generic"), new SchemaName("map"))
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "map");
        writer.WritePropertyName("values");
        ValueSchema.WriteTo(writer, writtenSchemas, containingNamespace);
        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
