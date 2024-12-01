using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Output;

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
            SchemaTypeTag.Enum => @default is not null ? $"{Name}.{Identifier.GetValid(@default.Value)}" : null,
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


    public static TypeSymbol Null(bool nullable) => nullable ? Types.NullNullable : Types.Null;
    public static TypeSymbol Boolean(bool nullable) => nullable ? Types.BooleanNullable : Types.Boolean;
    public static TypeSymbol Int(bool nullable) => nullable ? Types.IntNullable : Types.Int;
    public static TypeSymbol Long(bool nullable) => nullable ? Types.LongNullable : Types.Long;
    public static TypeSymbol Float(bool nullable) => nullable ? Types.FloatNullable : Types.Float;
    public static TypeSymbol Double(bool nullable) => nullable ? Types.DoubleNullable : Types.Double;
    public static TypeSymbol Bytes(bool nullable) => nullable ? Types.BytesNullable : Types.Bytes;
    public static TypeSymbol String(bool nullable) => nullable ? Types.StringNullable : Types.String;
    public static TypeSymbol Enum(JsonElement name, string @namespace, bool nullable) => new(SchemaTypeTag.Enum, nullable ? $"{@namespace}.{name}?" : $"{@namespace}.{name}");
    public static TypeSymbol Fixed(JsonElement name, string @namespace, bool nullable) => new(SchemaTypeTag.Fixed, nullable ? $"{@namespace}.{name}?" : $"{@namespace}.{name}");
    public static TypeSymbol Record(JsonElement name, string @namespace, bool nullable) => new(SchemaTypeTag.Record, nullable ? $"{@namespace}.{name}?" : $"{@namespace}.{name}");
    public static TypeSymbol Error(JsonElement name, string @namespace, bool nullable) => new(SchemaTypeTag.Error, nullable ? $"{@namespace}.{name}?" : $"{@namespace}.{name}");
    public static TypeSymbol Array(TypeSymbol typeArg, bool nullable) => new(SchemaTypeTag.Array, nullable ? $"System.Collections.Generic.IList<{typeArg.Name}>?" : $"System.Collections.Generic.IList<{typeArg.Name}>", typeArg);
    public static TypeSymbol Map(TypeSymbol typeArg, bool nullable) => new(SchemaTypeTag.Map, nullable ? $"System.Collections.Generic.IDictionary<string, {typeArg.Name}>?" : $"System.Collections.Generic.IDictionary<string, {typeArg.Name}>", typeArg);
    public static TypeSymbol Union(bool nullable) => nullable ? Types.UnionNullable : Types.Union;
    public static TypeSymbol LogicalDecimal(bool nullable) => nullable ? Types.LogicalDecimalNullable : Types.LogicalDecimal;
    public static TypeSymbol LogicalUuid(bool nullable) => nullable ? Types.LogicalUuidNullable : Types.LogicalUuid;
    public static TypeSymbol LogicalTimestampMillis(bool nullable) => nullable ? Types.LogicalTimestampMillisNullable : Types.LogicalTimestampMillis;
    public static TypeSymbol LogicalTimestampMicros(bool nullable) => nullable ? Types.LogicalTimestampMicrosNullable : Types.LogicalTimestampMicros;
    public static TypeSymbol LogicalLocalTimestampMillis(bool nullable) => nullable ? Types.LogicalLocalTimestampMillisNullable : Types.LogicalLocalTimestampMillis;
    public static TypeSymbol LogicalLocalTimestampMicros(bool nullable) => nullable ? Types.LogicalLocalTimestampMicrosNullable : Types.LogicalLocalTimestampMicros;
    public static TypeSymbol LogicalDate(bool nullable) => nullable ? Types.LogicalDateNullable : Types.LogicalDate;
    public static TypeSymbol LogicalTimeMillis(bool nullable) => nullable ? Types.LogicalTimeMillisNullable : Types.LogicalTimeMillis;
    public static TypeSymbol LogicalTimeMicros(bool nullable) => nullable ? Types.LogicalTimeMicrosNullable : Types.LogicalTimeMicros;
    public static TypeSymbol LogicalDuration(bool nullable) => nullable ? Types.LogicalDurationNullable : Types.LogicalDuration;

    public static TypeSymbol FromSchema(AvroSchema schema, bool nullable, SourceTextWriterOptions context) => context.Schemas.GetTypeTag(schema) switch
    {
        SchemaTypeTag.Null => Null(context.UseNullableReferenceTypes),
        SchemaTypeTag.Boolean => Boolean(nullable),
        SchemaTypeTag.Int => Int(nullable),
        SchemaTypeTag.Long => Long(nullable),
        SchemaTypeTag.Float => Float(nullable),
        SchemaTypeTag.Double => Double(nullable),
        SchemaTypeTag.Bytes => Bytes(nullable && context.UseNullableReferenceTypes),
        SchemaTypeTag.String => String(nullable && context.UseNullableReferenceTypes),
        SchemaTypeTag.Enum => Enum(schema.Name, context.Namespace, nullable),
        SchemaTypeTag.Fixed => Fixed(schema.Name, context.Namespace, nullable && context.UseNullableReferenceTypes),
        SchemaTypeTag.Record => Record(schema.Name, context.Namespace, nullable && context.UseNullableReferenceTypes),
        SchemaTypeTag.Error => Error(schema.Name, context.Namespace, nullable && context.UseNullableReferenceTypes),
        SchemaTypeTag.Array => FromArraySchema(schema.AsArraySchema(), nullable, context),
        SchemaTypeTag.Map => FromMapSchema(schema.AsMapSchema(), nullable, context),
        SchemaTypeTag.Union => FromUnionSchema(schema.AsUnionSchema(), context),
        SchemaTypeTag.Logical => FromLogicalSchema(schema.AsLogicalSchema(), nullable, context),
        _ => throw new InvalidOperationException($"Invalid schema '{schema.Name}' of type {context.Schemas.GetTypeTag(schema)}"),
    };

    private static TypeSymbol FromArraySchema(ArraySchema schema, bool nullable, SourceTextWriterOptions context)
    {
        var typeArg = FromSchema(schema.ItemsSchema, nullable: false, context);
        return Array(typeArg, nullable && context.UseNullableReferenceTypes);
    }

    private static TypeSymbol FromMapSchema(MapSchema schema, bool nullable, SourceTextWriterOptions context)
    {
        var typeArg = FromSchema(schema.ValuesSchema, nullable: false, context);
        return Map(typeArg, nullable && context.UseNullableReferenceTypes);
    }

    private static TypeSymbol FromUnionSchema(UnionSchema schema, SourceTextWriterOptions context)
    {
        var unionSchemas = schema.Schemas.ToArray();
        return unionSchemas.Length switch
        {
            1 => FromSchema(unionSchemas[0], nullable: false, context),
            2 => (context.Schemas.GetTypeTag(unionSchemas[0]), context.Schemas.GetTypeTag(unionSchemas[1])) switch
            {
                (SchemaTypeTag.Null, SchemaTypeTag.Null) => Union(context.UseNullableReferenceTypes),
                (_, SchemaTypeTag.Null) => FromSchema(unionSchemas[0], nullable: true, context),
                (SchemaTypeTag.Null, _) => FromSchema(unionSchemas[1], nullable: true, context),
                (_, _) => Union(context.UseNullableReferenceTypes),
            },
            _ => Union(unionSchemas.Any(s => context.Schemas.GetTypeTag(s) is SchemaTypeTag.Null) && context.UseNullableReferenceTypes)
        };
    }

    private static TypeSymbol FromLogicalSchema(LogicalSchema schema, bool nullable, SourceTextWriterOptions context) => schema.LogicalType.GetRawValue().Span switch
    {
    [0x22, 0x75, 0x75, 0x69, 0x64, 0x22] => LogicalUuid(nullable && context.UseNullableReferenceTypes),
    [0x22, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x6C, 0x6C, 0x69, 0x73, 0x22] => LogicalTimestampMillis(nullable),
    [0x22, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x22] => LogicalTimestampMicros(nullable),
    [0x22, 0x6C, 0x6F, 0x63, 0x61, 0x6C, 0x2D, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x6C, 0x6C, 0x69, 0x73, 0x22] => LogicalLocalTimestampMillis(nullable),
    [0x22, 0x6C, 0x6F, 0x63, 0x61, 0x6C, 0x2D, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x22] => LogicalLocalTimestampMicros(nullable),
    [0x22, 0x64, 0x61, 0x74, 0x65, 0x22] => LogicalDate(nullable),
    [0x22, 0x74, 0x69, 0x6D, 0x65, 0x2D, 0x6D, 0x69, 0x6C, 0x6C, 0x69, 0x73, 0x22] => LogicalTimeMillis(nullable),
    [0x22, 0x74, 0x69, 0x6D, 0x65, 0x2D, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x22] => LogicalTimeMicros(nullable),
    [0x22, 0x64, 0x75, 0x72, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x22] => LogicalDuration(nullable),
    [0x22, 0x64, 0x65, 0x63, 0x69, 0x6D, 0x61, 0x6C, 0x22] => LogicalDecimal(nullable),
        _ => throw new NotSupportedException(schema.LogicalType.GetRawText()),
    };
}

file static class Types
{
    public static readonly TypeSymbol Null = new(SchemaTypeTag.Null, "object");
    public static readonly TypeSymbol NullNullable = new(SchemaTypeTag.Null, "object?");
    public static readonly TypeSymbol Boolean = new(SchemaTypeTag.Null, "bool");
    public static readonly TypeSymbol BooleanNullable = new(SchemaTypeTag.Null, "bool?");
    public static readonly TypeSymbol Int = new(SchemaTypeTag.Null, "int");
    public static readonly TypeSymbol IntNullable = new(SchemaTypeTag.Null, "int?");
    public static readonly TypeSymbol Long = new(SchemaTypeTag.Null, "long");
    public static readonly TypeSymbol LongNullable = new(SchemaTypeTag.Null, "long?");
    public static readonly TypeSymbol Float = new(SchemaTypeTag.Null, "float");
    public static readonly TypeSymbol FloatNullable = new(SchemaTypeTag.Null, "float?");
    public static readonly TypeSymbol Double = new(SchemaTypeTag.Null, "double");
    public static readonly TypeSymbol DoubleNullable = new(SchemaTypeTag.Null, "double?");
    public static readonly TypeSymbol Bytes = new(SchemaTypeTag.Null, "byte[]");
    public static readonly TypeSymbol BytesNullable = new(SchemaTypeTag.Null, "byte[]?");
    public static readonly TypeSymbol String = new(SchemaTypeTag.Null, "string");
    public static readonly TypeSymbol StringNullable = new(SchemaTypeTag.Null, "string?");
    public static readonly TypeSymbol Union = new(SchemaTypeTag.Union, "object");
    public static readonly TypeSymbol UnionNullable = new(SchemaTypeTag.Union, "object?");
    public static readonly TypeSymbol LogicalDecimal = new(SchemaTypeTag.Logical, "Avro.AvroDecimal", null, "decimal");
    public static readonly TypeSymbol LogicalDecimalNullable = new(SchemaTypeTag.Logical, "Avro.AvroDecimal?", null, "decimal");
    public static readonly TypeSymbol LogicalUuid = new(SchemaTypeTag.Logical, "System.Guid", null, "uuid");
    public static readonly TypeSymbol LogicalUuidNullable = new(SchemaTypeTag.Logical, "System.Guid?", null, "uuid");
    public static readonly TypeSymbol LogicalTimestampMillis = new(SchemaTypeTag.Logical, "System.DateTime", null, "timestamp-millis");
    public static readonly TypeSymbol LogicalTimestampMillisNullable = new(SchemaTypeTag.Logical, "System.DateTime?", null, "timestamp-millis");
    public static readonly TypeSymbol LogicalTimestampMicros = new(SchemaTypeTag.Logical, "System.DateTime", null, "timestamp-micros");
    public static readonly TypeSymbol LogicalTimestampMicrosNullable = new(SchemaTypeTag.Logical, "System.DateTime?", null, "timestamp-micros");
    public static readonly TypeSymbol LogicalLocalTimestampMillis = new(SchemaTypeTag.Logical, "System.DateTime", null, "local-timestamp-millis");
    public static readonly TypeSymbol LogicalLocalTimestampMillisNullable = new(SchemaTypeTag.Logical, "System.DateTime?", null, "local-timestamp-millis");
    public static readonly TypeSymbol LogicalLocalTimestampMicros = new(SchemaTypeTag.Logical, "System.DateTime", null, "local-timestamp-micros");
    public static readonly TypeSymbol LogicalLocalTimestampMicrosNullable = new(SchemaTypeTag.Logical, "System.DateTime?", null, "local-timestamp-micros");
    public static readonly TypeSymbol LogicalDate = new(SchemaTypeTag.Logical, "System.DateTime", null, "date");
    public static readonly TypeSymbol LogicalDateNullable = new(SchemaTypeTag.Logical, "System.DateTime?", null, "date");
    public static readonly TypeSymbol LogicalTimeMillis = new(SchemaTypeTag.Logical, "System.TimeSpan", null, "time-millis");
    public static readonly TypeSymbol LogicalTimeMillisNullable = new(SchemaTypeTag.Logical, "System.TimeSpan?", null, "time-millis");
    public static readonly TypeSymbol LogicalTimeMicros = new(SchemaTypeTag.Logical, "System.TimeSpan", null, "time-micros");
    public static readonly TypeSymbol LogicalTimeMicrosNullable = new(SchemaTypeTag.Logical, "System.TimeSpan?", null, "time-micros");
    public static readonly TypeSymbol LogicalDuration = new(SchemaTypeTag.Logical, "int[]", null, "duration");
    public static readonly TypeSymbol LogicalDurationNullable = new(SchemaTypeTag.Logical, "int[]?", null, "duration");
}
