using System.Text.Json;

namespace AvroSourceGenerator.IntegrationTests;

internal sealed class JsonEqualityComparer<T> : IEqualityComparer<T>
{
    private static readonly JsonSerializerOptions s_options = new() { WriteIndented = true };

    public bool Equals(T? x, T? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;

        var xJson = JsonSerializer.Serialize(x, s_options);
        var yJson = JsonSerializer.Serialize(y, s_options);

        return xJson == yJson;
    }
    public int GetHashCode(T obj) => JsonSerializer.Serialize(obj, s_options).GetHashCode();
}
