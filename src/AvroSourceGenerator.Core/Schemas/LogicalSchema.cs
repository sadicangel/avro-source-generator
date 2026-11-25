using AvroSourceGenerator.Compiler.Configuration;

namespace AvroSourceGenerator.Schemas;

public sealed record class LogicalSchema(
    AvroSchema UnderlyingSchema,
    SchemaName SchemaName,
    CSharpName CSharpName)
    : AvroSchema(SchemaType.Logical, SchemaName, CSharpName, UnderlyingSchema.Documentation, UnderlyingSchema.Properties)
{
    public static AvroSchema Create(string logicalType, AvroSchema underlyingSchema, GenerationTarget generationTarget)
    {
        return generationTarget switch
        {
            GenerationTarget.Apache =>
                LogicalSchema.ForApache(logicalType, underlyingSchema),

            GenerationTarget.Legacy =>
                LogicalSchema.ForLegacy(logicalType, underlyingSchema),

            GenerationTarget.Modern =>
                LogicalSchema.ForModern(logicalType, underlyingSchema),

            _ => throw new InvalidOperationException($"Unsupported {nameof(GenerationTarget)} '{generationTarget}'"),
        };
    }
}
