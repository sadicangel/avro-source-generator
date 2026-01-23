using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Chr.RoundtripTests;

public class RecordsTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Records_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new TransactionEvent
        {
            Id = Guid.NewGuid(),
            Amount = 123.45m,
            Currency = "USD",
            Timestamp = DateTimeOffset.UtcNow.WithPrecisionLossFixed(),
            Status = TransactionStatus.COMPLETED,
            RecipientId = "123456",
            Metadata = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
            },
            Signature = new byte[64],
            LegacyId = "abc123",
        };

        Random.Shared.NextBytes(expected.Signature);

        var actual = await dockerFixture.RoundtripAsync(expected, TransactionEvent.GetSchema(), TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }
}
