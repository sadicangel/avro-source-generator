namespace AvroSourceGenerator.Schemas;

internal abstract record class AvroSchema(SchemaType Type, string Name, string? Namespace)
{
    public string FullName { get; } = Namespace is null ? Name : $"global::{Namespace}.{Name}";

    public sealed override string ToString() => FullName;

    public static readonly AvroSchema Object = new PrimitiveSchema(SchemaType.Null, "object", null);
    public static readonly AvroSchema Boolean = new PrimitiveSchema(SchemaType.Boolean, "bool", null);
    public static readonly AvroSchema Int = new PrimitiveSchema(SchemaType.Int, "int", null);
    public static readonly AvroSchema Long = new PrimitiveSchema(SchemaType.Long, "long", null);
    public static readonly AvroSchema Float = new PrimitiveSchema(SchemaType.Float, "float", null);
    public static readonly AvroSchema Double = new PrimitiveSchema(SchemaType.Double, "double", null);
    public static readonly AvroSchema Bytes = new PrimitiveSchema(SchemaType.Bytes, "byte[]", null);
    public static readonly AvroSchema String = new PrimitiveSchema(SchemaType.String, "string", null);

    public static readonly AvroSchema Date = new LogicalSchema("DateTime", "System");
    public static readonly AvroSchema Decimal = new LogicalSchema("AvroDecimal", "Avro");
    public static readonly AvroSchema TimeMillis = new LogicalSchema("TimeSpan", "System");
    public static readonly AvroSchema TimeMicros = new LogicalSchema("TimeSpan", "System");
    public static readonly AvroSchema TimestampMillis = new LogicalSchema("DateTime", "System");
    public static readonly AvroSchema TimestampMicros = new LogicalSchema("DateTime", "System");
    public static readonly AvroSchema LocalTimestampMillis = new LogicalSchema("DateTime", "System");
    public static readonly AvroSchema LocalTimestampMicros = new LogicalSchema("DateTime", "System");
    public static readonly AvroSchema Uuid = new LogicalSchema("Guid", "System");
}
