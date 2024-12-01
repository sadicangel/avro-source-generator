using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed class SchemaRegistry
{
    private readonly Dictionary<ReadOnlyMemory<byte>, AvroSchema> _schemas;

    public SchemaRegistry()
    {
        _schemas = new(new ReadOnlyMemoryComparer());
    }

    public bool TryRegister(AvroSchema schema)
    {
        var key = schema.Name.GetRawValue();
        if (_schemas.ContainsKey(key))
            return false;
        _schemas[key] = schema;
        return true;
    }

    public bool TryFind(ReadOnlyMemory<byte> key, out AvroSchema schema) => _schemas.TryGetValue(key, out schema);

    public SchemaTypeTag GetTypeTag<TSchema>(TSchema schema) where TSchema : IAvroSchema
    {
        return schema.Json.ValueKind switch
        {
            JsonValueKind.String => schema.Json.GetRawValue().Span switch
            {
            [0x22, 0x6E, 0x75, 0x6C, 0x6C, 0x22] => SchemaTypeTag.Null,
            [0x22, 0x62, 0x6F, 0x6F, 0x6C, 0x65, 0x61, 0x6E, 0x22] => SchemaTypeTag.Boolean,
            [0x22, 0x69, 0x6E, 0x74, 0x22] => SchemaTypeTag.Int,
            [0x22, 0x6C, 0x6F, 0x6E, 0x67, 0x22] => SchemaTypeTag.Long,
            [0x22, 0x66, 0x6C, 0x6F, 0x61, 0x74, 0x22] => SchemaTypeTag.Float,
            [0x22, 0x64, 0x6F, 0x75, 0x62, 0x6C, 0x65, 0x22] => SchemaTypeTag.Double,
            [0x22, 0x62, 0x79, 0x74, 0x65, 0x73, 0x22] => SchemaTypeTag.Bytes,
            [0x22, 0x73, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x22] => SchemaTypeTag.String,
                _ when TryFind(schema.Json.GetRawValue(), out var existingSchema) => GetTypeTag(existingSchema),
                _ => throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}")
            },
            JsonValueKind.Object => schema.Json.GetProperty("type").GetRawValue().Span switch
            {
            // Unnamed complex types.
            [0x22, 0x61, 0x72, 0x72, 0x61, 0x79, 0x22] => SchemaTypeTag.Array,
            [0x22, 0x6D, 0x61, 0x70, 0x22] => SchemaTypeTag.Map,
            // Named complex types.
            [0x22, 0x65, 0x6E, 0x75, 0x6D, 0x22] => SchemaTypeTag.Enum,
            [0x22, 0x72, 0x65, 0x63, 0x6F, 0x72, 0x64, 0x22] => SchemaTypeTag.Record,
            [0x22, 0x65, 0x72, 0x72, 0x6F, 0x72, 0x22] => SchemaTypeTag.Error,
            [0x22, 0x66, 0x69, 0x78, 0x65, 0x64, 0x22] => SchemaTypeTag.Fixed,
                _ when schema.Json.TryGetProperty("logicalType", out _) => SchemaTypeTag.Logical,
                _ => throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}")
            },
            JsonValueKind.Array => SchemaTypeTag.Union,
            _ => throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}")
        };
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