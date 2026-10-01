namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForModern(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalTypeNames.Date => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateOnly),
            LogicalTypeNames.Decimal => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.Decimal),
            LogicalTypeNames.Duration => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeSpan),
            LogicalTypeNames.TimeMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeOnly),
            LogicalTypeNames.TimeMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeOnly),
            LogicalTypeNames.TimestampMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTimeOffset),
            LogicalTypeNames.TimestampMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTimeOffset),
            LogicalTypeNames.LocalTimestampMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTimeOffset),
            LogicalTypeNames.LocalTimestampMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTimeOffset),
            LogicalTypeNames.Uuid => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.Guid),
            _ => underlyingSchema,
        };
    }
}
