using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Apache.RoundtripTests;

public class JavaExtensionsTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Types_with_java_extensions_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new JavaExtensions
        {
            default_class = "default-string",
            string_class = "java-string",
            stringable_class = "12345.67",
            default_map = new Dictionary<string, int>
            {
                ["one"] = 1,
                ["two"] = 2
            },
            string_map = new Dictionary<string, int>
            {
                ["alpha"] = 10,
                ["beta"] = 20
            },
            stringable_map = new Dictionary<string, int>
            {
                ["1.5"] = 100,
                ["2.75"] = 200
            }
        };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }
}
