namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForChr(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalType.Date => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateOnly", "System"),
                new SchemaName(logicalType)),
            LogicalType.Decimal => new LogicalSchema(
                underlyingSchema,
                new CSharpName("decimal"),
                new SchemaName(logicalType)),
            LogicalType.Duration => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeSpan", "System"),
                new SchemaName(logicalType)),
            LogicalType.TimeMicros => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeOnly", "System"),
                new SchemaName(logicalType)),
            LogicalType.TimeMillis => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeOnly", "System"),
                new SchemaName(logicalType)),
            LogicalType.TimestampMicros => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTimeOffset", "System"),
                new SchemaName(logicalType)),
            LogicalType.TimestampMillis => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTimeOffset", "System"),
                new SchemaName(logicalType)),
            LogicalType.LocalTimestampMicros => new LogicalSchema(
                underlyingSchema,
                underlyingSchema.CSharpName,
                new SchemaName(logicalType)),
            LogicalType.LocalTimestampMillis => new LogicalSchema(
                underlyingSchema,
                underlyingSchema.CSharpName,
                new SchemaName(logicalType)),
            LogicalType.Uuid => new LogicalSchema(
                underlyingSchema,
                new CSharpName("Guid", "System"),
                new SchemaName(logicalType)),
            _ => underlyingSchema,
        };
    }
}
