using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class PrimitiveSchema(SchemaType Type, CSharpName CSharpName, SchemaName SchemaName)
    : AvroSchema(Type, CSharpName, SchemaName)
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace) =>
        writer.WriteStringValue(SchemaName.Name);
}
