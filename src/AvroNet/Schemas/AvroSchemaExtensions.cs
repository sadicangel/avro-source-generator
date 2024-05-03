using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace AvroNet.Schemas;

internal static class AvroSchemaExtensions
{
#if false

    public static ReadOnlyMemory<byte> GetRawValue(this JsonElement jsonElement)
    {
        return GetRawValueFunc(jsonElement);

        // internal ReadOnlyMemory<byte> GetRawValue()
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetRawValue")]
        extern static ReadOnlyMemory<byte> GetRawValueFunc(JsonElement jsonElement);
    }
#else
    private static readonly Func<JsonElement, ReadOnlyMemory<byte>> GetRawValueFunc = CreateGetRawValueFunc();

    private static Func<JsonElement, ReadOnlyMemory<byte>> CreateGetRawValueFunc()
    {
        var parameter = Expression.Parameter(typeof(JsonElement));
        var method = typeof(JsonElement).GetMethod("GetRawValue", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var getRawValue =
            Expression.Lambda<Func<JsonElement, ReadOnlyMemory<byte>>>(
                Expression.Call(parameter, method),
                parameter)
            .Compile();

        return getRawValue;
    }

    public static ReadOnlyMemory<byte> GetRawValue(this JsonElement jsonElement) => GetRawValueFunc.Invoke(jsonElement);
#endif

    public static SchemaTypeTag GetTypeTag<TSchema>(this TSchema schema, IReadOnlyDictionary<ReadOnlyMemory<byte>, AvroSchema> schemas) where TSchema : IAvroSchema
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
                _ when schemas.TryGetValue(schema.Json.GetRawValue(), out var existingSchema) => existingSchema.GetTypeTag(schemas),
                _ => throw new InvalidOperationException($"Invalid schema {schema.Json.GetRawText()}")
            },
            JsonValueKind.Object => schema.Json.GetProperty("type").GetRawValue().Span switch
            {
            // Unnamed complex types.
            [0x22, 0x61, 0x72, 0x72, 0x61, 0x79, 0x22] => SchemaTypeTag.Array,
            [0x22, 0x6D, 0x61, 0x70, 0x22] => SchemaTypeTag.Map,
            // Named complex types.
            [0x22, 0x65, 0x6E, 0x75, 0x6D, 0x22] => SchemaTypeTag.Enumeration,
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