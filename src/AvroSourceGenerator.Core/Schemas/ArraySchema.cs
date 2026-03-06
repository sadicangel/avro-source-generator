using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class ArraySchema(AvroSchema ItemSchema, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType.Array, new CSharpName($"List<{ItemSchema}>", "System.Collections.Generic"), new SchemaName(AvroTypeNames.Array), Properties)
{
    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Array);
        writer.WritePropertyName(AvroJsonKeys.Items);
        ItemSchema.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
