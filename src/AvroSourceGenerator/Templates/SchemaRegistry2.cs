using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Templates;

internal sealed class SchemaRegistry2 : IEnumerable<AvroSchema>
{
    private readonly Dictionary<ReadOnlyMemory<byte>, AvroSchema> _schemas = new(new ReadOnlyMemoryComparer());
    private readonly Dictionary<ReadOnlyMemory<byte>, TypeSymbol2> _types = new(new ReadOnlyMemoryComparer());

    private readonly string _rootNamespace;
    private readonly bool _useNullableReferenceTypes;

    public SchemaRegistry2(AvroSchema rootSchema, bool useNullableReferenceTypes)
    {
        _rootNamespace = rootSchema.Json.TryGetProperty("namespace", out var ns) ? ns.GetString() ?? "" : "";
        _useNullableReferenceTypes = useNullableReferenceTypes;
        Type(rootSchema, nullable: false, registerNew: true);
    }

    public IEnumerator<AvroSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public TypeSymbol2 Type(AvroSchema schema) => Type(schema, nullable: false, registerNew: false);

    private void Register(AvroSchema schema, TypeSymbol2 type)
    {
        var name = schema.Name.GetRawValue();
        Debug.WriteLine(Encoding.UTF8.GetString(name.ToArray()));
        _schemas.Add(name, schema);
        _types.Add(name, type);
    }

    private TypeSymbol2 Type(AvroSchema schema, bool nullable, bool registerNew)
    {
        return schema.Json.ValueKind switch
        {
            // Primitive and registered types.
            JsonValueKind.String => KnownType(schema, nullable),
            // Complex types.
            JsonValueKind.Object => ComplexType(schema, nullable, registerNew),
            // Union type.
            JsonValueKind.Array => Union(schema.AsUnionSchema(), nullable, registerNew),

            _ => throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}")
        };
    }

    private TypeSymbol2 KnownType(AvroSchema schema, bool nullable)
    {
        return schema.Json.GetRawValue().Span switch
        {
        // "null"
        [0x22, 0x6E, 0x75, 0x6C, 0x6C, 0x22] => TypeSymbol2.Null(nullable & _useNullableReferenceTypes),
        // "boolean"
        [0x22, 0x62, 0x6F, 0x6F, 0x6C, 0x65, 0x61, 0x6E, 0x22] => TypeSymbol2.Boolean(nullable & _useNullableReferenceTypes),
        // "int"
        [0x22, 0x69, 0x6E, 0x74, 0x22] => TypeSymbol2.Int(nullable & _useNullableReferenceTypes),
        // "long"
        [0x22, 0x6C, 0x6F, 0x6E, 0x67, 0x22] => TypeSymbol2.Long(nullable & _useNullableReferenceTypes),
        // "float"
        [0x22, 0x66, 0x6C, 0x6F, 0x61, 0x74, 0x22] => TypeSymbol2.Float(nullable & _useNullableReferenceTypes),
        // "double"
        [0x22, 0x64, 0x6F, 0x75, 0x62, 0x6C, 0x65, 0x22] => TypeSymbol2.Double(nullable & _useNullableReferenceTypes),
        // "bytes"
        [0x22, 0x62, 0x79, 0x74, 0x65, 0x73, 0x22] => TypeSymbol2.Bytes(nullable & _useNullableReferenceTypes),
        // "string"
        [0x22, 0x73, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x22] => TypeSymbol2.String(nullable & _useNullableReferenceTypes),
            // Registered types.
            _ when _types.TryGetValue(schema.Json.GetRawValue(), out var registeredType) => registeredType,
            // Invalid schema.
            _ =>
                throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}")
        };
    }

    private TypeSymbol2 ComplexType(AvroSchema schema, bool nullable, bool registerNew)
    {
        return schema.Json.GetProperty("type").GetRawValue().Span switch
        {
            // Logical types.
            _ when schema.Json.TryGetProperty("logicalType", out _) => Logical(schema.AsLogicalSchema(), nullable),
            // "array"
            [0x22, 0x61, 0x72, 0x72, 0x61, 0x79, 0x22] => Array(schema.AsArraySchema(), nullable, registerNew),
            // "map"
            [0x22, 0x6D, 0x61, 0x70, 0x22] => Map(schema.AsMapSchema(), nullable, registerNew),
            // "enum"
            [0x22, 0x65, 0x6E, 0x75, 0x6D, 0x22] => Enum(schema.AsEnumSchema(), nullable, registerNew),
            // "record"
            [0x22, 0x72, 0x65, 0x63, 0x6F, 0x72, 0x64, 0x22] => Record(schema.AsRecordSchema(), nullable, registerNew),
            // "error"
            [0x22, 0x65, 0x72, 0x72, 0x6F, 0x72, 0x22] => Error(schema.AsErrorSchema(), nullable, registerNew),
            // "fixed"
            [0x22, 0x66, 0x69, 0x78, 0x65, 0x64, 0x22] => Fixed(schema.AsFixedSchema(), nullable, registerNew),
            // Invalid schema.
            _ => throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}")
        };
    }

    private TypeSymbol2 Array(ArraySchema schema, bool nullable, bool registerNew)
    {
        var typeArg = Type(schema.ItemsSchema, nullable: false, registerNew);

        return TypeSymbol2.Array(typeArg, nullable & _useNullableReferenceTypes);
    }

    private TypeSymbol2 Map(MapSchema schema, bool nullable, bool registerNew)
    {
        var typeArg = Type(schema.ValuesSchema, nullable: false, registerNew);

        return TypeSymbol2.Map(typeArg, nullable & _useNullableReferenceTypes);
    }

    private TypeSymbol2 Enum(EnumSchema schema, bool nullable, bool registerNew)
    {
        _ = _useNullableReferenceTypes; // Value types can always be nullable.

        if (_types.TryGetValue(schema.Name.GetRawValue(), out var enumType))
            return enumType;

        if (!registerNew)
            throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}");

        enumType = TypeSymbol2.Enum(schema.Name, nullable, _rootNamespace);
        Register(schema, enumType);

        return enumType;
    }

    private TypeSymbol2 Record(RecordSchema schema, bool nullable, bool registerNew)
    {
        if (_types.TryGetValue(schema.Name.GetRawValue(), out var recordType))
            return recordType;

        if (!registerNew)
            throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}");

        recordType = TypeSymbol2.Record(schema.Name, nullable & _useNullableReferenceTypes, _rootNamespace);
        Register(schema, recordType);

        foreach (var field in schema.Fields)
            Type(field.Schema, nullable: false, registerNew);

        return recordType;
    }

    private TypeSymbol2 Error(ErrorSchema schema, bool nullable, bool registerNew)
    {
        if (_types.TryGetValue(schema.Name.GetRawValue(), out var errorType))
            return errorType;

        if (!registerNew)
            throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}");

        errorType = TypeSymbol2.Error(schema.Name, nullable & _useNullableReferenceTypes, _rootNamespace);
        Register(schema, errorType);

        foreach (var field in schema.Fields)
            Type(field.Schema, nullable: false, registerNew);

        return errorType;
    }

    private TypeSymbol2 Fixed(FixedSchema schema, bool nullable, bool registerNew)
    {
        if (_types.TryGetValue(schema.Name.GetRawValue(), out var fixedType))
            return fixedType;

        if (!registerNew)
            throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}");

        fixedType = TypeSymbol2.Fixed(schema.Name, nullable & _useNullableReferenceTypes, _rootNamespace);
        Register(schema, fixedType);

        return fixedType;
    }

    private TypeSymbol2 Logical(LogicalSchema schema, bool nullable)
    {
        _ = _rootNamespace;  // Logical types are built-in and do not have a namespace.
        _ = _useNullableReferenceTypes; // Logical types are always nullable, since they are translated value types.

        return schema.LogicalType.GetRawValue().Span switch
        {
        // "uuid"
        [0x22, 0x75, 0x75, 0x69, 0x64, 0x22] => TypeSymbol2.LogicalUuid(nullable),
        // "timestamp-millis"
        [0x22, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x6C, 0x6C, 0x69, 0x73, 0x22] => TypeSymbol2.LogicalTimestampMillis(nullable),
        // "timestamp-micros"
        [0x22, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x22] => TypeSymbol2.LogicalTimestampMicros(nullable),
        // "local-timestamp-millis"
        [0x22, 0x6C, 0x6F, 0x63, 0x61, 0x6C, 0x2D, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x6C, 0x6C, 0x69, 0x73, 0x22] => TypeSymbol2.LogicalLocalTimestampMillis(nullable),
        // "local-timestamp-micros"
        [0x22, 0x6C, 0x6F, 0x63, 0x61, 0x6C, 0x2D, 0x74, 0x69, 0x6D, 0x65, 0x73, 0x74, 0x61, 0x6D, 0x70, 0x2D, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x22] => TypeSymbol2.LogicalLocalTimestampMicros(nullable),
        // "date"
        [0x22, 0x64, 0x61, 0x74, 0x65, 0x22] => TypeSymbol2.LogicalDate(nullable),
        // "time-millis"
        [0x22, 0x74, 0x69, 0x6D, 0x65, 0x2D, 0x6D, 0x69, 0x6C, 0x6C, 0x69, 0x73, 0x22] => TypeSymbol2.LogicalTimeMillis(nullable),
        // "time-micros"
        [0x22, 0x74, 0x69, 0x6D, 0x65, 0x2D, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x22] => TypeSymbol2.LogicalTimeMicros(nullable),
        // "duration"
        [0x22, 0x64, 0x75, 0x72, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x22] => TypeSymbol2.LogicalDuration(nullable),
        // "decimal"
        [0x22, 0x64, 0x65, 0x63, 0x69, 0x6D, 0x61, 0x6C, 0x22] => TypeSymbol2.LogicalDecimal(nullable),
            // Invalid logical type.
            _ => throw new NotSupportedException(schema.LogicalType.GetRawText()),
        };
    }

    private TypeSymbol2 Union(UnionSchema schema, bool nullable, bool registerNew)
    {
        // TODO: Do not use array here. Use span or pooled array instead.
        var types = schema.Schemas.Select(schema => Type(schema, nullable: false, registerNew)).ToArray();

        return types switch
        {
        // Empty union. This probably should not happen.
        [] => TypeSymbol2.Null(nullable & _useNullableReferenceTypes),
        // Single type.
        [var type] => type,
        // "null" | "null"
        [var first and { Kind: SchemaTypeTag.Null }, var second and { Kind: SchemaTypeTag.Null }] => TypeSymbol2.Union(_useNullableReferenceTypes),
        // "null" | T
        [var first and { Kind: SchemaTypeTag.Null }, var second] => MakeNullable(second, _useNullableReferenceTypes),
        // T | "null"
        [var first, var second and { Kind: SchemaTypeTag.Null }] => MakeNullable(first, _useNullableReferenceTypes),
            // T1 | T2 | ... | Tn
            _ => TypeSymbol2.Union(types.Any(t => t.IsNullable) && _useNullableReferenceTypes)
        };

        static TypeSymbol2 MakeNullable(TypeSymbol2 type, bool useNullableReferenceTypes)
        {
            if (!useNullableReferenceTypes || type.IsNullable) return type;
            return type with { Name = $"{type.Name}?" };
        }
    }
}

file sealed class ReadOnlyMemoryComparer : IEqualityComparer<ReadOnlyMemory<byte>>
{
    public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y) => x.Span.SequenceEqual(y.Span);
    public int GetHashCode([DisallowNull] ReadOnlyMemory<byte> obj)
    {
        var hash = new HashCode();
#if NET5_0_OR_GREATER
        hash.AddBytes(obj.Span);
#else
        foreach (var b in obj.Span)
            hash.Add(b);
#endif
        return hash.ToHashCode();
    }
}
