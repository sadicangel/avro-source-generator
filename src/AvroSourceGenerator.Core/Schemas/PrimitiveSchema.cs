using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class PrimitiveSchema(SchemaType Type, SchemaName SchemaName, CSharpName CSharpName, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(Type, SchemaName, CSharpName, Documentation, Properties)
{
    public PrimitiveSchema(SchemaType type, CSharpName csharpName, SchemaName schemaName)
        : this(type, schemaName, csharpName, Documentation: null, Properties: ImmutableSortedDictionary<string, JsonElement>.Empty) { }

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
