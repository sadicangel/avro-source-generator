using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class LogicalSchema(
    AvroSchema UnderlyingSchema,
    CSharpName CSharpName,
    SchemaName SchemaName)
    : AvroSchema(SchemaType.Logical, CSharpName, SchemaName, UnderlyingSchema.Properties)
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        var underlyingSchema = UnderlyingSchema with { Properties = Properties.Add("logicalType", JsonElement.Parse($"\"{SchemaName.Name}\"")) };
        underlyingSchema.WriteTo(writer, writtenSchemas, containingNamespace);
    }
}
