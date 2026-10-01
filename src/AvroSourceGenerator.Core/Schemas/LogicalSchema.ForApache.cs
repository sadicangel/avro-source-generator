namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForApache(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalTypeNames.Date => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.Decimal when underlyingSchema.Type is SchemaType.Bytes => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.AvroDecimal),
            // Apache parses fixed-backed decimal as AvroDecimal, but its specific datum IO
            // currently rejects the GenericFixed produced by the decimal converter.
            LogicalTypeNames.Decimal when underlyingSchema.Type is SchemaType.Fixed => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.AvroDecimal),
            LogicalTypeNames.Decimal => underlyingSchema,
            // Apache.Avro serializes fixed-backed duration as a nested type that Schema Registry rejects.
            LogicalTypeNames.Duration when underlyingSchema.Type is SchemaType.Fixed => underlyingSchema,
            LogicalTypeNames.TimeMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeSpan),
            LogicalTypeNames.TimeMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.TimeSpan),
            LogicalTypeNames.TimestampMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.TimestampMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.LocalTimestampMicros => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.LocalTimestampMillis => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.DateTime),
            LogicalTypeNames.Uuid when underlyingSchema.Type is SchemaType.String => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), CSharpName.Guid),
            // Apache.Avro 1.12.2 still rejects uuid on fixed, despite accepting unknown logical types.
            LogicalTypeNames.Uuid => underlyingSchema,
            _ => new LogicalSchema(underlyingSchema, new SchemaName(logicalType), underlyingSchema.CSharpName),
        };
    }

    extension(CSharpName)
    {
        public static CSharpName AvroDecimal => new CSharpName("AvroDecimal", "Avro");
    }
}
