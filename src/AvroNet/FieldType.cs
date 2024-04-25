using Avro;

namespace AvroNet;

internal sealed record class FieldType(string Name, FieldType? TypeArg = null, string? LogicalType = null)
{
    public bool IsNullable { get => Name[^1] == '?'; }
    public bool IsLogicalType { get => LogicalType is not null; }

    public override string ToString() => Name;

    public static FieldType Null { get; } = new("object?");
    public static FieldType Boolean { get; } = new("bool");
    public static FieldType BooleanNullable { get; } = new("bool?");
    public static FieldType Int { get; } = new("int");
    public static FieldType IntNullable { get; } = new("int?");
    public static FieldType Long { get; } = new("long");
    public static FieldType LongNullable { get; } = new("long?");
    public static FieldType Float { get; } = new("float");
    public static FieldType FloatNullable { get; } = new("float?");
    public static FieldType Double { get; } = new("double");
    public static FieldType DoubleNullable { get; } = new("double?");
    public static FieldType Bytes { get; } = new("byte[]");
    public static FieldType BytesNullable { get; } = new("byte[]?");
    public static FieldType String { get; } = new("string");
    public static FieldType StringNullable { get; } = new("string?");
    public static FieldType Type(string name, string @namespace) => new($"{@namespace}.{name}");
    public static FieldType TypeNullable(string name, string @namespace) => new($"{@namespace}.{name}?");
    public static FieldType Array(FieldType typeArg) => new($"System.Collections.Generic.IList<{typeArg.Name}>", typeArg);
    public static FieldType ArrayNullable(FieldType typeArg) => new($"System.Collections.Generic.IList<{typeArg.Name}>?", typeArg);
    public static FieldType Map(FieldType typeArg) => new($"System.Collections.Generic.IDictionary<string, {typeArg.Name}>", typeArg);
    public static FieldType MapNullable(FieldType typeArg) => new($"System.Collections.Generic.IDictionary<string, {typeArg.Name}>?", typeArg);
    public static FieldType Union { get; } = new("object");
    public static FieldType UnionNullable { get; } = new("object?");
    public static FieldType LogicalDecimal { get; } = new("Avro.Decimal", null, "decimal");
    public static FieldType LogicalDecimalNullable { get; } = new("Avro.Decimal?", null, "decimal");
    public static FieldType LogicalUuid { get; } = new("System.Guid", null, "uuid");
    public static FieldType LogicalUuidNullable { get; } = new("System.Guid?", null, "uuid");
    public static FieldType LogicalTimestampMillis { get; } = new("System.DateTime", null, "timestamp-millis");
    public static FieldType LogicalTimestampMillisNullable { get; } = new("System.DateTime?", null, "timestamp-millis");
    public static FieldType LogicalTimestampMicros { get; } = new("System.DateTime", null, "timestamp-micros");
    public static FieldType LogicalTimestampMicrosNullable { get; } = new("System.DateTime?", null, "timestamp-micros");
    public static FieldType LogicalLocalTimestampMillis { get; } = new("System.DateTime", null, "local-timestamp-millis");
    public static FieldType LogicalLocalTimestampMillisNullable { get; } = new("System.DateTime?", null, "local-timestamp-millis");
    public static FieldType LogicalLocalTimestampMicros { get; } = new("System.DateTime", null, "local-timestamp-micros");
    public static FieldType LogicalLocalTimestampMicrosNullable { get; } = new("System.DateTime?", null, "local-timestamp-micros");
    public static FieldType LogicalDate { get; } = new("System.DateTime", null, "date");
    public static FieldType LogicalDateNullable { get; } = new("System.DateTime?", null, "date");
    public static FieldType LogicalTimeMillis { get; } = new("System.TimeSpan", null, "time-millis");
    public static FieldType LogicalTimeMillisNullable { get; } = new("System.TimeSpan?", null, "time-millis");
    public static FieldType LogicalTimeMicros { get; } = new("System.TimeSpan", null, "time-micros");
    public static FieldType LogicalTimeMicrosNullable { get; } = new("System.TimeSpan?", null, "time-micros");
    public static FieldType LogicalDuration { get; } = new FieldType("int[]", null, "duration");
    public static FieldType LogicalDurationNullable { get; } = new FieldType("int[]?", null, "duration");

    public static FieldType FromSchema(Schema schema, string @namespace, bool nullable = false) => schema.Tag switch
    {
        Schema.Type.Null => Null,
        Schema.Type.Boolean => nullable ? BooleanNullable : Boolean,
        Schema.Type.Int => nullable ? IntNullable : Int,
        Schema.Type.Long => nullable ? LongNullable : Long,
        Schema.Type.Float => nullable ? FloatNullable : Float,
        Schema.Type.Double => nullable ? DoubleNullable : Double,
        Schema.Type.Bytes => nullable ? BytesNullable : Bytes,
        Schema.Type.String => nullable ? StringNullable : String,
        Schema.Type.Enumeration => nullable ? TypeNullable(schema.Name, @namespace) : Type(schema.Name, @namespace),
        Schema.Type.Fixed => nullable ? TypeNullable(schema.Name, @namespace) : Type(schema.Name, @namespace),
        Schema.Type.Record => nullable ? TypeNullable(schema.Name, @namespace) : Type(schema.Name, @namespace),
        Schema.Type.Error => nullable ? TypeNullable(schema.Name, @namespace) : Type(schema.Name, @namespace),
        Schema.Type.Array => FromArraySchema((ArraySchema)schema, @namespace, nullable),
        Schema.Type.Map => FromMapSchema((MapSchema)schema, @namespace, nullable),
        Schema.Type.Union => FromUnionSchema((UnionSchema)schema, @namespace),
        Schema.Type.Logical => FromLogicalSchema((LogicalSchema)schema, @namespace, nullable),
        _ => throw new CodeGenException($"Invalid schema '{schema.Name}' of type {schema.Tag}"),
    };

    private static FieldType FromArraySchema(ArraySchema schema, string @namespace, bool nullable)
    {
        var typeArg = FromSchema(schema.ItemSchema, @namespace);
        return nullable ? ArrayNullable(typeArg) : Array(typeArg);
    }

    private static FieldType FromMapSchema(MapSchema schema, string @namespace, bool nullable)
    {
        var typeArg = FromSchema(schema.ValueSchema, @namespace);
        return nullable ? MapNullable(typeArg) : Map(typeArg);
    }

    private static FieldType FromUnionSchema(UnionSchema schema, string @namespace)
    {
        return schema.Count switch
        {
            1 => FromSchema(schema.Schemas[0], @namespace),
            2 => (schema.Schemas[0].Tag, schema.Schemas[1].Tag) switch
            {
                (Schema.Type.Null, Schema.Type.Null) => UnionNullable,
                (_, Schema.Type.Null) => FromSchema(schema.Schemas[0], @namespace, nullable: true),
                (Schema.Type.Null, _) => FromSchema(schema.Schemas[1], @namespace, nullable: true),
                (_, _) => Union,
            },
            _ => schema.Schemas.Any(s => s.Tag is Schema.Type.Null) ? UnionNullable : Union
        };
    }

    private static FieldType FromLogicalSchema(LogicalSchema schema, string @namespace, bool nullable) => schema.LogicalTypeName switch
    {
        "uuid" => nullable ? LogicalUuidNullable : LogicalUuid,
        "timestamp-millis" => nullable ? LogicalTimestampMillisNullable : LogicalTimestampMillis,
        "timestamp-micros" => nullable ? LogicalTimestampMicrosNullable : LogicalTimestampMicros,
        "local-timestamp-millis" => nullable ? LogicalLocalTimestampMillisNullable : LogicalLocalTimestampMillis,
        "local-timestamp-micros" => nullable ? LogicalLocalTimestampMicrosNullable : LogicalLocalTimestampMicros,
        "date" => nullable ? LogicalDateNullable : LogicalDate,
        "time-millis" => nullable ? LogicalTimeMillisNullable : LogicalTimeMillis,
        "time-micros" => nullable ? LogicalTimeMicrosNullable : LogicalTimeMicros,
        "duration" => nullable ? LogicalDurationNullable : LogicalDuration,
        "decimal" => nullable ? LogicalDecimalNullable : LogicalDecimal,
        _ => throw new NotSupportedException(schema.LogicalTypeName),
    };
}