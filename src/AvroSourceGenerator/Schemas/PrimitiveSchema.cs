using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal record class PrimitiveSchema(SchemaType Type, CSharpName CSharpName, SchemaName SchemaName)
    : AvroSchema(Type, CSharpName, SchemaName)
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace) =>
        writer.WriteStringValue(SchemaName.Name);

    public AvroSchema WithProperties(ImmutableSortedDictionary<string, JsonElement> properties)
    {
        if (properties.IsEmpty)
            return this;
        return new PrimitiveSchemaWithProperties(this, properties);
    }
}

internal record PrimitiveSchemaWithProperties(SchemaType Type, CSharpName CSharpName, SchemaName SchemaName, ImmutableSortedDictionary<string, JsonElement> Properties) : AvroSchema(Type, CSharpName, SchemaName)
{
    public PrimitiveSchemaWithProperties(PrimitiveSchema type, ImmutableSortedDictionary<string, JsonElement> Properties) : this(type.Type, type.CSharpName, type.SchemaName, Properties)
    {
    }

    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        WriteProperties(Properties, writer);
        writer.WritePropertyName("type");
        writer.WriteStringValue(SchemaName.Name);
        writer.WriteEndObject();
    }

    private static void WriteProperties(ImmutableSortedDictionary<string, JsonElement> properties, Utf8JsonWriter writer)
    {
        foreach (var entry in properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }
    }
}
