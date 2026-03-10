using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class PrimitiveSchema(SchemaType Type, CSharpName CSharpName, SchemaName SchemaName, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(Type, CSharpName, SchemaName, Documentation, Properties)
{
    public PrimitiveSchema(SchemaType type, CSharpName csharpName, SchemaName schemaName)
        : this(type, csharpName, schemaName, Documentation: null, ImmutableSortedDictionary<string, JsonElement>.Empty) { }

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        if (Properties.IsEmpty)
        {
            writer.WriteStringValue(SchemaName.Name);
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName(AvroJsonKeys.Type);
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
