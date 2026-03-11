using System.Text.Json;
using AvroSourceGenerator.Configuration;

namespace AvroSourceGenerator.Schemas;

public sealed record class LogicalSchema(
    AvroSchema UnderlyingSchema,
    CSharpName CSharpName,
    SchemaName SchemaName)
    : AvroSchema(SchemaType.Logical, CSharpName, SchemaName, UnderlyingSchema.Documentation, UnderlyingSchema.Properties)
{
    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        var logicalType = JsonSerializer.SerializeToElement(SchemaName.Name);
        var underlyingSchema = UnderlyingSchema with { Properties = Properties.Add(AvroJsonKeys.LogicalType, logicalType) };
        underlyingSchema.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
    }

    public static AvroSchema Create(string logicalType, AvroSchema underlyingSchema, TargetProfile targetProfile)
    {
        return targetProfile switch
        {
            TargetProfile.Apache =>
                LogicalSchema.ForApache(logicalType, underlyingSchema),

            TargetProfile.Chr =>
                LogicalSchema.ForChr(logicalType, underlyingSchema),

            TargetProfile.Legacy =>
                LogicalSchema.ForLegacy(logicalType, underlyingSchema),

            TargetProfile.Modern =>
                LogicalSchema.ForModern(logicalType, underlyingSchema),

            _ => throw new InvalidOperationException($"Unsupported {nameof(TargetProfile)} '{targetProfile}'"),
        };
    }
}
