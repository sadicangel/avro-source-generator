using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Apache.RoundtripTests;

public class RecordsTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Records_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new TransactionEvent
        {
            Id = Guid.NewGuid(),
            Amount = new Avro.AvroDecimal(123.45m),
            Currency = "USD",
            Timestamp = DateTime.UtcNow.WithPrecisionLossFixed(),
            Status = TransactionStatus.COMPLETED,
            RecipientId = "123456",
            Metadata = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
            },
            Signature = new Signature(),
            LegacyId = "abc123",
        };

        Random.Shared.NextBytes(expected.Signature.Value);

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }
}
