namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForChr(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalTypeNames.Date => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateOnly),
            LogicalTypeNames.Decimal => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.Decimal),
            LogicalTypeNames.Duration => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeSpan),
            LogicalTypeNames.TimeMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeOnly),
            LogicalTypeNames.TimeMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeOnly),
            LogicalTypeNames.TimestampMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTimeOffset),
            LogicalTypeNames.TimestampMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTimeOffset),
            LogicalTypeNames.LocalTimestampMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), underlyingSchema.CSharpName),
            LogicalTypeNames.LocalTimestampMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), underlyingSchema.CSharpName),
            LogicalTypeNames.Uuid => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.Guid),
            // TODO: Is this correct? Shouldn't we wrap the underlying schema in a LogicalSchema for unknown logical types?
            _ => underlyingSchema,
        };
    }
}
