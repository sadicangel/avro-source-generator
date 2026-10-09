using System.Text.Json;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class SerializationAssertions
{
    private static JsonSerializerOptions CreateOptions() => JsonSerializerOptions.Default;
}
