namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForApache(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalTypeNames.Date => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("DateTime", "System")),
            // Apache parses fixed-backed decimal as AvroDecimal, but its specific datum IO
            // currently rejects the GenericFixed produced by the decimal converter.
            LogicalTypeNames.Decimal when underlyingSchema.Type is SchemaType.Bytes or SchemaType.Fixed => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("AvroDecimal", "Avro")),
            LogicalTypeNames.Decimal => underlyingSchema,
            // Apache.Avro serializes fixed-backed duration as a nested type that Schema Registry rejects.
            LogicalTypeNames.Duration when underlyingSchema.Type is SchemaType.Fixed => underlyingSchema,
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
            LogicalTypeNames.Uuid when underlyingSchema.Type is SchemaType.String => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                new CSharpName("Guid", "System")),
            // Apache.Avro 1.12.2 still rejects uuid on fixed, despite accepting unknown logical types.
            LogicalTypeNames.Uuid => underlyingSchema,
            _ => new LogicalSchema(
                underlyingSchema,
                new SchemaName(logicalType),
                underlyingSchema.CSharpName),
        };
    }
}
