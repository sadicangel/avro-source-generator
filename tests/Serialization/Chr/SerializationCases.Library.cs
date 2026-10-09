using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class SerializationCases
{
    public static LogicalTypes CreateLogicalTypes()
    {
        var expected = new LogicalTypes
        {
            birthDate = new DateOnly(1990, 1, 1),
            price = 99.99m,
            subscriptionPeriod = TimeSpan.FromMilliseconds(43200000).Add(TimeSpan.FromDays(2)),
            checkInTime = new TimeOnly(9, 0, 0),
            preciseCheckInTime = new TimeOnly(0, 0, 0, 10),
            createdAt = new DateTimeOffset(s_timestamp),
            updatedAt = new DateTimeOffset(s_timestamp),
            localPublishedTime = (long)(new DateTimeOffset(s_timestamp) - DateTimeOffset.UnixEpoch).TotalMilliseconds,
            localEditedTime = (long)(new DateTimeOffset(s_timestamp) - DateTimeOffset.UnixEpoch).TotalMicroseconds,
            sessionId = s_id,
            futureLabel = "plain string",
        };
        return expected;
    }

    public static TransactionEvent CreateRecords()
    {
        var expected = new TransactionEvent
        {
            Id = s_id,
            Amount = 123.45m,
            Currency = "USD",
            Timestamp = new DateTimeOffset(s_timestamp),
            Status = TransactionStatus.COMPLETED,
            RecipientId = "123456",
            Metadata = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
            },
            Signature = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            LegacyId = "abc123",
        };
        return expected;
    }

    private static PaymentRecord CreateFixedPayment() => new PaymentRecord { paymentMethod = s_id.ToByteArray() };
}
