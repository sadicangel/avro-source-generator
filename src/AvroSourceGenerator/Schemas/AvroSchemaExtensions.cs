using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

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
    private static readonly Func<JsonElement, ReadOnlyMemory<byte>> s_getRawValueFunc = CreateGetRawValueFunc();

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

    public static ReadOnlyMemory<byte> GetRawValue(this JsonElement jsonElement) => s_getRawValueFunc.Invoke(jsonElement);
#endif
}
