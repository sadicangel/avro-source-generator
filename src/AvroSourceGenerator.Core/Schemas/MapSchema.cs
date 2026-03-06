using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class MapSchema(AvroSchema ValueSchema, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType.Map, new CSharpName($"Dictionary<string, {ValueSchema}>", "System.Collections.Generic"), new SchemaName(AvroTypeNames.Map), Properties)
{
    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Map);
        writer.WritePropertyName(AvroJsonKeys.Values);
        ValueSchema.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
