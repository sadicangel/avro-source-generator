namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForLegacy(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalTypeNames.Date => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType)),
            LogicalTypeNames.Decimal => new LogicalSchema(
                underlyingSchema,
                new CSharpName("decimal"),
                new SchemaName(logicalType)),
            LogicalTypeNames.Duration => new LogicalSchema(
                underlyingSchema,
                underlyingSchema.CSharpName,
                new SchemaName(logicalType)),
            LogicalTypeNames.TimeMicros => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeSpan", "System"),
                new SchemaName(logicalType)),
            LogicalTypeNames.TimeMillis => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeSpan", "System"),
                new SchemaName(logicalType)),
            LogicalTypeNames.TimestampMicros => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType)),
            LogicalTypeNames.TimestampMillis => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType)),
            LogicalTypeNames.LocalTimestampMicros => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType)),
            LogicalTypeNames.LocalTimestampMillis => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType)),
            LogicalTypeNames.Uuid => new LogicalSchema(
                underlyingSchema,
                new CSharpName("Guid", "System"),
                new SchemaName(logicalType)),
            _ => underlyingSchema,
        };
    }
}
