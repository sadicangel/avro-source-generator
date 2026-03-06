namespace AvroSourceGenerator.Schemas;

internal static partial class LogicalSchemaExtensions
{
    extension(LogicalSchema)
    {
        public static AvroSchema ForApache(string logicalType, AvroSchema underlyingSchema) => logicalType switch
        {
            LogicalTypeNames.Date => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType)),
            LogicalTypeNames.Decimal when underlyingSchema.Type is SchemaType.Bytes => new LogicalSchema(
                underlyingSchema,
                new CSharpName("AvroDecimal", "Avro"),
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
            LogicalTypeNames.Uuid when underlyingSchema.Type is SchemaType.String => new LogicalSchema(
                underlyingSchema,
                new CSharpName("Guid", "System"),
                new SchemaName(logicalType)),

            // TODO: Language implementations must ignore unknown logical types when reading, and should use the underlying Avro type.
            // Apache.Avro will throw an exception when it encounters an unknown logical type, which is not compliant.
            // We avoid this behaviour in AvroSourceGenerator by erasing unsupported logical types from the schema before
            // passing it to Avro.Schema.Parse. Although this workaround works for code generation, it will probably cause
            // issues with validating schemas against schema registries that return schemas with unsupported logical types.
            // 
            // These PRs seem to be trying to address this issue:
            // https://github.com/apache/avro/pull/2512
            // https://github.com/apache/avro/pull/2751
            _ => underlyingSchema,
        };
    }
}
