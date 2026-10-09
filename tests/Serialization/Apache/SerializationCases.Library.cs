using Avro;
using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class SerializationCases
{
    public static LogicalTypes CreateLogicalTypes()
    {
        var expected = new LogicalTypes
        {
            birthDate = new DateOnly(1990, 1, 1).ToDateTime(default, DateTimeKind.Utc),
            price = new AvroDecimal(99.99m),
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
            createdAt = s_timestamp,
            updatedAt = s_timestamp,
            localPublishedTime = DateTime.SpecifyKind(s_timestamp, DateTimeKind.Local),
            localEditedTime = DateTime.SpecifyKind(s_timestamp, DateTimeKind.Local),
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
            Amount = new AvroDecimal(123.45m),
            Currency = "USD",
            Timestamp = s_timestamp,
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

        expected.Signature.Value = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
        return expected;
    }

    private static PaymentRecord CreateFixedPayment() => new PaymentRecord { paymentMethod = new PaymentMethodId { Value = s_id.ToByteArray() } };
}
