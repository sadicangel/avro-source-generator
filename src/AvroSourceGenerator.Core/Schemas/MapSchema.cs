using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class MapSchema(AvroSchema ValueSchema, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType.Map, GetCSharpName(ValueSchema), new SchemaName(AvroTypeNames.Map), Documentation, Properties)
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

    private static CSharpName GetCSharpName(AvroSchema valueSchema) => new($"Dictionary<string, {valueSchema}>", "System.Collections.Generic");
}
