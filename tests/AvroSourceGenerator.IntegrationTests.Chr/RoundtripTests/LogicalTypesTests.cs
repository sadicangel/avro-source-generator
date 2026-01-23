using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Chr.RoundtripTests;

public class LogicalTypesTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Logical_types_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new LogicalTypes
        {
            birthDate = new DateOnly(1990, 1, 1),
            price = 99.99m,
            //taxRate = 0.15m,
            subscriptionPeriod = TimeSpan.FromMilliseconds(43200000).Add(TimeSpan.FromDays(2)),
            checkInTime = new TimeOnly(9, 0, 0),
            preciseCheckInTime = new TimeOnly(0, 0, 0, 10),
            createdAt = DateTimeOffset.UtcNow.WithPrecisionLossFixed(),
            updatedAt = DateTimeOffset.UtcNow.WithPrecisionLossFixed(),
            localPublishedTime = (long)(DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).TotalMilliseconds,
            localEditedTime = (long)(DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).TotalMicroseconds,
            sessionId = Guid.NewGuid(),
            //paymentTransaction = Guid.NewGuid(),
        };

        var actual = await dockerFixture.RoundtripAsync(expected, LogicalTypes.GetSchema(), TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }
}
