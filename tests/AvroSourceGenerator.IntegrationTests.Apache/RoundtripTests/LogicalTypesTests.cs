using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Apache.RoundtripTests;

public class LogicalTypesTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Logical_types_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new LogicalTypes
        {
            birthDate = new DateOnly(1990, 1, 1).ToDateTime(default, DateTimeKind.Utc),
            price = new Avro.AvroDecimal(99.99m),
            //taxRate = new Avro.AvroDecimal(0.15m),
            subscriptionPeriod = new SubscriptionDuration
            {
                Value =
                [
                    0x00, 0x00, 0x00, 0x00, // Months = 0
                    0x02, 0x00, 0x00, 0x00, // Days = 2
                    0x00, 0x2E, 0x93, 0x02, // Milliseconds = 43,200,000
                ]
            },
            checkInTime = TimeSpan.FromHours(9),
            preciseCheckInTime = TimeSpan.FromMicroseconds(10000),
            createdAt = DateTime.UtcNow.WithPrecisionLossFixed(),
            updatedAt = DateTime.UtcNow.WithPrecisionLossFixed(),
            localPublishedTime = DateTime.Now.WithPrecisionLossFixed(),
            localEditedTime = DateTime.Now.WithPrecisionLossFixed(),
            sessionId = Guid.NewGuid(),
            //paymentTransaction = Guid.NewGuid(),
        };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }
}
