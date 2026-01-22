using System.Text.Json;

namespace AvroSourceGenerator.IntegrationTests;

public sealed record JsonEqualityComparer<T>(JsonSerializerOptions JsonOptions) : IEqualityComparer<T>
{
    private string Serialize(T value) => JsonSerializer.Serialize(value, JsonOptions);

    public bool Equals(T? x, T? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;

        var xJson = Serialize(x);
        var yJson = Serialize(y);

        return xJson == yJson;
    }

    public int GetHashCode(T obj) => Serialize(obj).GetHashCode();
}
