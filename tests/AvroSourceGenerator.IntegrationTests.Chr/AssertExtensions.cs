using System.Text.Json;

namespace AvroSourceGenerator.IntegrationTests.Chr;

public static class AssertExtensions
{
    extension(Assert)
    {
        // To avoid reference type comparison, use JSON.
        public static void EqualAsJson<T>(T expected, T actual) =>
            Assert.Equal(expected, actual, new JsonEqualityComparer<T>(JsonSerializerOptions.Default));
    }
}
