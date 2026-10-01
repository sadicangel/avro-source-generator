namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForLegacy(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalTypeNames.Date => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.Decimal => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.Decimal),
            LogicalTypeNames.Duration => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), underlyingSchema.CSharpName),
            LogicalTypeNames.TimeMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeSpan),
            LogicalTypeNames.TimeMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeSpan),
            LogicalTypeNames.TimestampMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.TimestampMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.LocalTimestampMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.LocalTimestampMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.Uuid => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.Guid),
            _ => underlyingSchema,
        };
    }
}
