using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class ArraySchema(AvroSchema ItemSchema, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType.Array, GetCSharpName(ItemSchema), new SchemaName(AvroTypeNames.Array), Documentation, Properties)
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

    private static CSharpName GetCSharpName(AvroSchema itemSchema) => new CSharpName($"List<{itemSchema}>", "System.Collections.Generic");
}
