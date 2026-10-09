using System.Text.Json;
using Xunit;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class SerializationAssertions
{
    private static readonly JsonSerializerOptions s_options = CreateOptions();

    public static void Equal<T>(T expected, T actual)
    {
        Assert.Equal(JsonSerializer.Serialize(expected, s_options), JsonSerializer.Serialize(actual, s_options));
    }
}
