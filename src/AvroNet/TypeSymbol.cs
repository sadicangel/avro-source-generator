using AvroNet.Schemas;
using System.Text.Json;

namespace AvroNet;

internal sealed record class TypeSymbol(SchemaTypeTag Tag, string Name, TypeSymbol? TypeArg = null, string? LogicalType = null)
{
    public bool IsNullable { get => Name[^1] == '?'; }

    public override string ToString() => Name;

    public string? GetValue(JsonElement? @default)
    {
        return Tag switch
        {
            SchemaTypeTag.Null => @default?.GetRawText(),
            SchemaTypeTag.Boolean => @default?.GetRawText(),
            SchemaTypeTag.Int => @default?.GetRawText(),
            SchemaTypeTag.Long => @default?.GetRawText(),
            SchemaTypeTag.Float => @default?.GetRawText(),
            SchemaTypeTag.Double => @default?.GetRawText(),
            SchemaTypeTag.Bytes => @default?.GetRawText() is string bytes ? $"System.Text.Encoding.GetBytes({bytes})" : null,
            SchemaTypeTag.String => @default?.GetRawText(),
            SchemaTypeTag.Enumeration => @default is not null ? $"{Name}.{Identifier.GetValid(@default.Value)}" : null,
            SchemaTypeTag.Fixed => @default?.GetRawText() is string bytes ? $"new {Name} {{ Value = System.Text.Encoding.GetBytes({bytes}) }}" : null,
            //SchemaTypeTag.Union => throw new NotImplementedException(),
            //SchemaTypeTag.Record => throw new NotImplementedException(),
            //SchemaTypeTag.Array => throw new NotImplementedException(),
            //SchemaTypeTag.Map => throw new NotImplementedException(),
            //SchemaTypeTag.Error => throw new NotImplementedException(),
            //SchemaTypeTag.Logical => throw new NotImplementedException(),
            _ => null,
        };
    }

    public static TypeSymbol Null { get; } = new(SchemaTypeTag.Null, "object?");
    public static TypeSymbol Boolean { get; } = new(SchemaTypeTag.Boolean, "bool");
    public static TypeSymbol BooleanNullable { get; } = new(SchemaTypeTag.Boolean, "bool?");
    public static TypeSymbol Int { get; } = new(SchemaTypeTag.Int, "int");
    public static TypeSymbol IntNullable { get; } = new(SchemaTypeTag.Int, "int?");
    public static TypeSymbol Long { get; } = new(SchemaTypeTag.Long, "long");
    public static TypeSymbol LongNullable { get; } = new(SchemaTypeTag.Long, "long?");
    public static TypeSymbol Float { get; } = new(SchemaTypeTag.Float, "float");
    public static TypeSymbol FloatNullable { get; } = new(SchemaTypeTag.Float, "float?");
    public static TypeSymbol Double { get; } = new(SchemaTypeTag.Double, "double");
    public static TypeSymbol DoubleNullable { get; } = new(SchemaTypeTag.Double, "double?");
    public static TypeSymbol Bytes { get; } = new(SchemaTypeTag.Bytes, "byte[]");
    public static TypeSymbol BytesNullable { get; } = new(SchemaTypeTag.Bytes, "byte[]?");
    public static TypeSymbol String { get; } = new(SchemaTypeTag.String, "string");
    public static TypeSymbol StringNullable { get; } = new(SchemaTypeTag.String, "string?");
    public static TypeSymbol Enum(JsonElement name, string @namespace) => new(SchemaTypeTag.Enumeration, $"{@namespace}.{name}");
    public static TypeSymbol EnumNullable(JsonElement name, string @namespace) => new(SchemaTypeTag.Enumeration, $"{@namespace}.{name}?");
    public static TypeSymbol Fixed(JsonElement name, string @namespace) => new(SchemaTypeTag.Fixed, $"{@namespace}.{name}");
    public static TypeSymbol FixedNullable(JsonElement name, string @namespace) => new(SchemaTypeTag.Fixed, $"{@namespace}.{name}?");
    public static TypeSymbol Record(JsonElement name, string @namespace) => new(SchemaTypeTag.Record, $"{@namespace}.{name}");
    public static TypeSymbol RecordNullable(JsonElement name, string @namespace) => new(SchemaTypeTag.Record, $"{@namespace}.{name}?");
    public static TypeSymbol Error(JsonElement name, string @namespace) => new(SchemaTypeTag.Error, $"{@namespace}.{name}");
    public static TypeSymbol ErrorNullable(JsonElement name, string @namespace) => new(SchemaTypeTag.Error, $"{@namespace}.{name}?");
    public static TypeSymbol Array(TypeSymbol typeArg) => new(SchemaTypeTag.Array, $"System.Collections.Generic.IList<{typeArg.Name}>", typeArg);
    public static TypeSymbol ArrayNullable(TypeSymbol typeArg) => new(SchemaTypeTag.Array, $"System.Collections.Generic.IList<{typeArg.Name}>?", typeArg);
    public static TypeSymbol Map(TypeSymbol typeArg) => new(SchemaTypeTag.Map, $"System.Collections.Generic.IDictionary<string, {typeArg.Name}>", typeArg);
    public static TypeSymbol MapNullable(TypeSymbol typeArg) => new(SchemaTypeTag.Map, $"System.Collections.Generic.IDictionary<string, {typeArg.Name}>?", typeArg);
    public static TypeSymbol Union { get; } = new(SchemaTypeTag.Union, "object");
    public static TypeSymbol UnionNullable { get; } = new(SchemaTypeTag.Union, "object?");
    public static TypeSymbol LogicalDecimal { get; } = new(SchemaTypeTag.Logical, "Avro.AvroDecimal", null, "decimal");
    public static TypeSymbol LogicalDecimalNullable { get; } = new(SchemaTypeTag.Logical, "Avro.AvroDecimal?", null, "decimal");
    public static TypeSymbol LogicalUuid { get; } = new(SchemaTypeTag.Logical, "System.Guid", null, "uuid");
    public static TypeSymbol LogicalUuidNullable { get; } = new(SchemaTypeTag.Logical, "System.Guid?", null, "uuid");
    public static TypeSymbol LogicalTimestampMillis { get; } = new(SchemaTypeTag.Logical, "System.DateTime", null, "timestamp-millis");
    public static TypeSymbol LogicalTimestampMillisNullable { get; } = new(SchemaTypeTag.Logical, "System.DateTime?", null, "timestamp-millis");
    public static TypeSymbol LogicalTimestampMicros { get; } = new(SchemaTypeTag.Logical, "System.DateTime", null, "timestamp-micros");
    public static TypeSymbol LogicalTimestampMicrosNullable { get; } = new(SchemaTypeTag.Logical, "System.DateTime?", null, "timestamp-micros");
    public static TypeSymbol LogicalLocalTimestampMillis { get; } = new(SchemaTypeTag.Logical, "System.DateTime", null, "local-timestamp-millis");
    public static TypeSymbol LogicalLocalTimestampMillisNullable { get; } = new(SchemaTypeTag.Logical, "System.DateTime?", null, "local-timestamp-millis");
    public static TypeSymbol LogicalLocalTimestampMicros { get; } = new(SchemaTypeTag.Logical, "System.DateTime", null, "local-timestamp-micros");
    public static TypeSymbol LogicalLocalTimestampMicrosNullable { get; } = new(SchemaTypeTag.Logical, "System.DateTime?", null, "local-timestamp-micros");
    public static TypeSymbol LogicalDate { get; } = new(SchemaTypeTag.Logical, "System.DateTime", null, "date");
    public static TypeSymbol LogicalDateNullable { get; } = new(SchemaTypeTag.Logical, "System.DateTime?", null, "date");
    public static TypeSymbol LogicalTimeMillis { get; } = new(SchemaTypeTag.Logical, "System.TimeSpan", null, "time-millis");
    public static TypeSymbol LogicalTimeMillisNullable { get; } = new(SchemaTypeTag.Logical, "System.TimeSpan?", null, "time-millis");
    public static TypeSymbol LogicalTimeMicros { get; } = new(SchemaTypeTag.Logical, "System.TimeSpan", null, "time-micros");
    public static TypeSymbol LogicalTimeMicrosNullable { get; } = new(SchemaTypeTag.Logical, "System.TimeSpan?", null, "time-micros");
    public static TypeSymbol LogicalDuration { get; } = new(SchemaTypeTag.Logical, "int[]", null, "duration");
    public static TypeSymbol LogicalDurationNullable { get; } = new(SchemaTypeTag.Logical, "int[]?", null, "duration");

    public static TypeSymbol FromSchema(
        AvroSchema schema,
        bool nullable,
        IReadOnlyDictionary<ReadOnlyMemory<byte>, AvroSchema> schemas,
        AvroModelOptions options) => schema.GetTypeTag(schemas) switch
        {
            SchemaTypeTag.Null => Null,
            SchemaTypeTag.Boolean => nullable ? BooleanNullable : Boolean,
            SchemaTypeTag.Int => nullable ? IntNullable : Int,
            SchemaTypeTag.Long => nullable ? LongNullable : Long,
            SchemaTypeTag.Float => nullable ? FloatNullable : Float,
            SchemaTypeTag.Double => nullable ? DoubleNullable : Double,
            SchemaTypeTag.Bytes => nullable && options.UseNullableReferenceTypes ? BytesNullable : Bytes,
            SchemaTypeTag.String => nullable && options.UseNullableReferenceTypes ? StringNullable : String,
            SchemaTypeTag.Enumeration => nullable ? EnumNullable(schema.Name, options.Namespace) : Enum(schema.Name, options.Namespace),
            SchemaTypeTag.Fixed => nullable && options.UseNullableReferenceTypes ? FixedNullable(schema.Name, options.Namespace) : Fixed(schema.Name, options.Namespace),
            SchemaTypeTag.Record => nullable && options.UseNullableReferenceTypes ? RecordNullable(schema.Name, options.Namespace) : Record(schema.Name, options.Namespace),
            SchemaTypeTag.Error => nullable && options.UseNullableReferenceTypes ? ErrorNullable(schema.Name, options.Namespace) : Error(schema.Name, options.Namespace),
            SchemaTypeTag.Array => FromArraySchema(schema.AsArraySchema(), nullable, schemas, options),
            SchemaTypeTag.Map => FromMapSchema(schema.AsMapSchema(), nullable, schemas, options),
            SchemaTypeTag.Union => FromUnionSchema(schema.AsUnionSchema(), schemas, options),
            SchemaTypeTag.Logical => FromLogicalSchema(schema.AsLogicalSchema(), nullable, options),
            _ => throw new InvalidOperationException($"Invalid schema '{schema.Name}' of type {schema.GetTypeTag(schemas)}"),
        };

    private static TypeSymbol FromArraySchema(
        ArraySchema schema,
        bool nullable,
        IReadOnlyDictionary<ReadOnlyMemory<byte>, AvroSchema> schemas,
        AvroModelOptions options)
    {
        var typeArg = FromSchema(schema.ItemsSchema, nullable: false, schemas, options);
        return nullable && options.UseNullableReferenceTypes ? ArrayNullable(typeArg) : Array(typeArg);
    }

    private static TypeSymbol FromMapSchema(
        MapSchema schema,
        bool nullable,
        IReadOnlyDictionary<ReadOnlyMemory<byte>, AvroSchema> schemas,
        AvroModelOptions options)
    {
        var typeArg = FromSchema(schema.ValuesSchema, nullable: false, schemas, options);
        return nullable && options.UseNullableReferenceTypes ? MapNullable(typeArg) : Map(typeArg);
    }

    private static TypeSymbol FromUnionSchema(
        UnionSchema schema,
        IReadOnlyDictionary<ReadOnlyMemory<byte>, AvroSchema> schemas,
        AvroModelOptions options)
    {
        var unionSchemas = schema.Schemas.ToArray();
        return unionSchemas.Length switch
        {
            1 => FromSchema(unionSchemas[0], nullable: false, schemas, options),
            2 => (unionSchemas[0].GetTypeTag(schemas), unionSchemas[1].GetTypeTag(schemas)) switch
            {
                (SchemaTypeTag.Null, SchemaTypeTag.Null) => UnionNullable,
                (_, SchemaTypeTag.Null) => FromSchema(unionSchemas[0], nullable: true, schemas, options),
                (SchemaTypeTag.Null, _) => FromSchema(unionSchemas[1], nullable: true, schemas, options),
                (_, _) => Union,
            },
            _ => unionSchemas.Any(s => s.GetTypeTag(schemas) is SchemaTypeTag.Null) && options.UseNullableReferenceTypes ? UnionNullable : Union
        };
    }

    private static TypeSymbol FromLogicalSchema(LogicalSchema schema, bool nullable, AvroModelOptions options) => schema.LogicalType.GetRawValue().Span switch
    {
    [0x22, 0x75, 0x75, 0x69, 0x64, 0x22] => nullable && options.UseNullableReferenceTypes ? LogicalUuidNullable : LogicalUuid,
    [0x22, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x6C, 0x6C, 0x69, 0x73, 0x22] => nullable ? LogicalTimestampMillisNullable : LogicalTimestampMillis,
    [0x22, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x22] => nullable ? LogicalTimestampMicrosNullable : LogicalTimestampMicros,
    [0x22, 0x6C, 0x6F, 0x63, 0x61, 0x6C, 0x2D, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x6C, 0x6C, 0x69, 0x73, 0x22] => nullable ? LogicalLocalTimestampMillisNullable : LogicalLocalTimestampMillis,
    [0x22, 0x6C, 0x6F, 0x63, 0x61, 0x6C, 0x2D, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x22] => nullable ? LogicalLocalTimestampMicrosNullable : LogicalLocalTimestampMicros,
    [0x22, 0x64, 0x61, 0x74, 0x65, 0x22] => nullable ? LogicalDateNullable : LogicalDate,
    [0x22, 0x74, 0x69, 0x6D, 0x65, 0x2D, 0x6D, 0x69, 0x6C, 0x6C, 0x69, 0x73, 0x22] => nullable ? LogicalTimeMillisNullable : LogicalTimeMillis,
    [0x22, 0x74, 0x69, 0x6D, 0x65, 0x2D, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x22] => nullable ? LogicalTimeMicrosNullable : LogicalTimeMicros,
    [0x22, 0x64, 0x75, 0x72, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x22] => nullable ? LogicalDurationNullable : LogicalDuration,
    [0x22, 0x64, 0x65, 0x63, 0x69, 0x6D, 0x61, 0x6C, 0x22] => nullable ? LogicalDecimalNullable : LogicalDecimal,
        _ => throw new NotSupportedException(schema.LogicalType.GetRawText()),
    };
}