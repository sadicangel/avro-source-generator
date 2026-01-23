using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Chr.RoundtripTests;

public class EnumsTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Enums_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new Enums
        {
            status = Status.ACTIVE,
            nullableStatus1 = Status.INACTIVE,
            nullableStatus2 = null,
        };
        var actual = await dockerFixture.RoundtripAsync(expected, Enums.GetSchema(), TestContext.Current.CancellationToken);
        Assert.EqualAsJson(expected, actual);
    }
}
