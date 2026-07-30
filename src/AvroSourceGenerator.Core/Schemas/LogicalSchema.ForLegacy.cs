namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForLegacy(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalTypeNames.Date => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTime", "System")),
            LogicalTypeNames.Decimal => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("decimal")),
            LogicalTypeNames.Duration => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                underlyingSchema.CSharpName),
            LogicalTypeNames.TimeMicros => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("TimeSpan", "System")),
            LogicalTypeNames.TimeMillis => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("TimeSpan", "System")),
            LogicalTypeNames.TimestampMicros => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTime", "System")),
            LogicalTypeNames.TimestampMillis => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTime", "System")),
            LogicalTypeNames.LocalTimestampMicros => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTime", "System")),
            LogicalTypeNames.LocalTimestampMillis => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTime", "System")),
            LogicalTypeNames.Uuid => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("Guid", "System")),
            _ => underlyingSchema,
        };
    }
}
