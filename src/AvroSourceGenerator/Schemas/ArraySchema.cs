using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class ArraySchema(AvroSchema ItemSchema, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType.Array, new CSharpName($"List<{ItemSchema}>", "System.Collections.Generic"), new SchemaName("array"), Properties)
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "array");
        writer.WritePropertyName("items");
        ItemSchema.WriteTo(writer, writtenSchemas, containingNamespace);
        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
