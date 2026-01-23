using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Apache.RoundtripTests;

public class CollectionsTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Collections_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new Collections
        {
            stringList = ["Hello", "Avro", "!"],
            intMap = new Dictionary<string, int>
            {
                ["Hello"] = 1,
                ["Avro"] = 2,
                ["!"] = 3
            },
        };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }
}
