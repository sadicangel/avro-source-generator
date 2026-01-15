using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForApache(
            string logicalType,
            AvroSchema underlyingSchema,
            ImmutableSortedDictionary<string, JsonElement> properties) => logicalType switch
            {
                LogicalType.Date => new LogicalSchema(
                    underlyingSchema,
                    new CSharpName("DateTime", "System"),
                    new SchemaName(logicalType),
                    properties),
                LogicalType.Decimal => new LogicalSchema(
                    underlyingSchema,
                    new CSharpName("AvroDecimal", "Avro"),
                    new SchemaName(logicalType),
                    properties),
                LogicalType.Duration => new LogicalSchema(
                    underlyingSchema,
                    underlyingSchema.CSharpName,
                    new SchemaName(logicalType),
                    properties),
                LogicalType.TimeMicros => new LogicalSchema(
                    underlyingSchema,
                    new CSharpName("TimeSpan", "System"),
                    new SchemaName(logicalType),
                    properties),
                LogicalType.TimeMillis => new LogicalSchema(
                    underlyingSchema,
                    new CSharpName("TimeSpan", "System"),
                    new SchemaName(logicalType),
                    properties),
                LogicalType.TimestampMicros => new LogicalSchema(
                    underlyingSchema,
                    new CSharpName("DateTime", "System"),
                    new SchemaName(logicalType),
                    properties),
                LogicalType.TimestampMillis => new LogicalSchema(
                    underlyingSchema,
                    new CSharpName("DateTime", "System"),
                    new SchemaName(logicalType),
                    properties),
                LogicalType.LocalTimestampMicros => new LogicalSchema(
                    underlyingSchema,
                    new CSharpName("DateTime", "System"),
                    new SchemaName(logicalType),
                    properties),
                LogicalType.LocalTimestampMillis => new LogicalSchema(
                    underlyingSchema,
                    new CSharpName("DateTime", "System"),
                    new SchemaName(logicalType),
                    properties),
                LogicalType.Uuid => new LogicalSchema(
                    underlyingSchema,
                    new CSharpName("Guid", "System"),
                    new SchemaName(logicalType),
                    properties),
                _ => underlyingSchema,
            };
    }
}
