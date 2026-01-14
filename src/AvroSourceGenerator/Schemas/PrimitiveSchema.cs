using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class PrimitiveSchema(SchemaType Type, CSharpName CSharpName, SchemaName SchemaName, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(Type, CSharpName, SchemaName)
{
    public PrimitiveSchema(SchemaType type, CSharpName csharpName, SchemaName schemaName)
        : this(type, csharpName, schemaName, ImmutableSortedDictionary<string, JsonElement>.Empty) { }

    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        if (Properties.IsEmpty)
        {
            writer.WriteStringValue(SchemaName.Name);
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            writer.WriteStringValue(SchemaName.Name);
            foreach (var entry in Properties)
            {
                writer.WritePropertyName(entry.Key);
                entry.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
    }
}
