namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForModern(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalTypeNames.Date => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateOnly", "System")),
            LogicalTypeNames.Decimal => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("decimal")),
            LogicalTypeNames.Duration => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("TimeSpan", "System")),
            LogicalTypeNames.TimeMicros => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("TimeOnly", "System")),
            LogicalTypeNames.TimeMillis => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("TimeOnly", "System")),
            LogicalTypeNames.TimestampMicros => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTimeOffset", "System")),
            LogicalTypeNames.TimestampMillis => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTimeOffset", "System")),
            LogicalTypeNames.LocalTimestampMicros => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTimeOffset", "System")),
            LogicalTypeNames.LocalTimestampMillis => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTimeOffset", "System")),
            LogicalTypeNames.Uuid => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("Guid", "System")),
            _ => underlyingSchema,
        };
    }
}
