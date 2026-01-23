using System.Text.Json;
using Avro.Specific;

namespace AvroSourceGenerator.IntegrationTests.Apache;

public static class AssertExtensions
{
    extension(Assert)
    {
        // To avoid reference type comparison, use JSON.
        public static void EqualAsJson<T>(T expected, T actual) where T : ISpecificRecord =>
            Assert.Equal(expected, actual, new JsonEqualityComparer<T>(new JsonSerializerOptions { Converters = { new FixedJsonConverterFactory() } }));
    }
}
