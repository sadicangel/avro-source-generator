using System.Text.Json;
using AvroSourceGenerator.Compiler;

namespace AvroSourceGenerator.Schemas;

public sealed record class LogicalSchema(
    AvroSchema UnderlyingSchema,
    SchemaName SchemaName,
    CSharpName CSharpName)
    : AvroSchema(SchemaType.Logical, SchemaName, CSharpName, UnderlyingSchema.Documentation, UnderlyingSchema.Properties)
{
    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        var logicalType = JsonSerializer.SerializeToElement(SchemaName.Name);
        var underlyingSchema = UnderlyingSchema with { Properties = Properties.Add(AvroJsonKeys.LogicalType, logicalType) };
        underlyingSchema.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
    }

    public static AvroSchema Create(string logicalType, AvroSchema underlyingSchema, GenerationTarget generationTarget)
    {
        return generationTarget switch
        {
            GenerationTarget.Apache =>
                LogicalSchema.ForApache(logicalType, underlyingSchema),

            GenerationTarget.Chr =>
                LogicalSchema.ForChr(logicalType, underlyingSchema),

            GenerationTarget.Legacy =>
                LogicalSchema.ForLegacy(logicalType, underlyingSchema),

            GenerationTarget.Modern =>
                LogicalSchema.ForModern(logicalType, underlyingSchema),

            _ => throw new InvalidOperationException($"Unsupported {nameof(GenerationTarget)} '{generationTarget}'"),
        };
    }
}
