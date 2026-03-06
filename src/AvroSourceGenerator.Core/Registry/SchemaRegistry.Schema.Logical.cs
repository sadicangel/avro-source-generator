using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private AvroSchema Logical(JsonElement schema, AvroSchema underlyingSchema)
    {
        var logicalType = schema.GetRequiredString(AvroJsonKeys.LogicalType);

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
