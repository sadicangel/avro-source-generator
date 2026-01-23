using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class LogicalSchema(
    AvroSchema UnderlyingSchema,
    CSharpName CSharpName,
    SchemaName SchemaName)
    : AvroSchema(SchemaType.Logical, CSharpName, SchemaName, UnderlyingSchema.Properties)
{
    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        var logicalType = JsonSerializer.SerializeToElement(SchemaName.Name);
        var underlyingSchema = UnderlyingSchema with { Properties = Properties.Add("logicalType", logicalType) };
        underlyingSchema.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
    }
}
