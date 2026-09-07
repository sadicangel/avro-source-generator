using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Apache;

public class ImportsTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Imported_schema_types_remain_unchanged_after_roundtrip_to_kafka()
    {
        var item = new NestedType { displayName = "Imported" };
        var expected = new ImportedEnvelope { Item = item };

        var actual = await dockerFixture.RoundtripAsync(
            expected,
            TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }
}
