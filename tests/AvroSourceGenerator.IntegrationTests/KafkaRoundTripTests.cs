using System.Text;
using Avro.Specific;
using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

public class KafkaRoundTripTests(DockerFixture dockerFixture)
{
    // To avoid reference type comparison, use JSON.
    private static void AssertEqual<T>(T expected, T actual) where T : ISpecificRecord =>
        Assert.Equal(expected, actual, new JsonEqualityComparer<T>());

    // Avoid precision loss when converting to underlying type.
    private static DateTime FixPrecisionLoss(DateTime dateTime) =>
        dateTime.Date + TimeSpan.FromMilliseconds((long)dateTime.TimeOfDay.TotalMilliseconds);

    [Fact]
    public async Task Primitives_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new Primitives
        {
            intField = 42,
            longField = 42L,
            floatField = 42.42f,
            doubleField = 42.42,
            boolField = true,
            stringField = "Hello, Avro!",
            bytesField = Encoding.UTF8.GetBytes("Hello, Avro!"),

            nullableInt1 = 42,
            nullableLong1 = 42L,
            nullableFloat1 = 42.42f,
            nullableDouble1 = 42.42,
            nullableBool1 = true,
            nullableString1 = "Hello, Avro!",
            nullableBytes1 = Encoding.UTF8.GetBytes("Hello, Avro!"),

            nullableInt2 = null,
            nullableLong2 = null,
            nullableFloat2 = null,
            nullableDouble2 = null,
            nullableBool2 = null,
            nullableString2 = null,
            nullableBytes2 = null,
        };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        AssertEqual(expected, actual);
    }

    [Fact]
    public async Task Collections_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new Collections
        {
            stringList = ["Hello", "Avro", "!"],
            intMap = new Dictionary<string, int> { ["Hello"] = 1, ["Avro"] = 2, ["!"] = 3 },
        };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        AssertEqual(expected, actual);
    }

    [Fact]
    public async Task Enums_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new Enums
        {
            status = Status.ACTIVE,
            nullableStatus1 = Status.INACTIVE,
            nullableStatus2 = null,
        };
        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);
        AssertEqual(expected, actual);
    }

    [Fact]
    public async Task Records_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new TransactionEvent
        {
            Id = Guid.NewGuid(),
            Amount = new Avro.AvroDecimal(123.45m),
            Currency = "USD",
            Timestamp = FixPrecisionLoss(DateTime.UtcNow),
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

        AssertEqual(expected, actual);
    }

    [Fact]
    public async Task Logical_types_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new LogicalTypes
        {
            birthDate = new DateOnly(1990, 1, 1).ToDateTime(default, DateTimeKind.Utc),
            price = new Avro.AvroDecimal(99.99m),
            //taxRate = new Avro.AvroDecimal(0.15m),
            //subscriptionPeriod = new SubscriptionDuration { Months = 1, Days = 2, Milliseconds = 43200000 },
            checkInTime = TimeSpan.FromHours(9),
            preciseCheckInTime = TimeSpan.FromMicroseconds(10000),
            createdAt = FixPrecisionLoss(DateTime.UtcNow),
            updatedAt = FixPrecisionLoss(DateTime.UtcNow),
            localPublishedTime = FixPrecisionLoss(DateTime.Now),
            localEditedTime = FixPrecisionLoss(DateTime.Now),
            sessionId = Guid.NewGuid(),
            //paymentTransaction = Guid.NewGuid(),
        };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        AssertEqual(expected, actual);
    }

    [Theory]
    [InlineData(null), InlineData(2), InlineData("cash"), MemberData(nameof(CreditCardPaymentVariant))]
    public async Task Union_types_mapped_to_object_remain_unchanged_after_roundtrip_to_kafka(object? variant)
    {
        var expected = new PaymentRecord
        {
            paymentMethod = variant
        };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        AssertEqual(expected, actual);
    }

    public static TheoryData<CreditCardPayment> CreditCardPaymentVariant() => [
        new CreditCardPayment
        {
            cardNumber = "4111111111111111",
            cardHolder = "Alice Example",
            expirationDate = "12/26",
        }
    ];

    [Theory]
    [MemberData(nameof(NotificationVariants))]
    public async Task Union_types_mapped_to_abstract_remain_unchanged_after_roundtrip_to_kafka(NotificationContentVariant content)
    {
        var expected = new Notification { content = content };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        AssertEqual(expected, actual);
    }

    public static TheoryData<NotificationContentVariant> NotificationVariants() => [
        new EmailContent
        {
            subject = "Welcome!",
            body = "Thanks for signing up.",
            recipientEmail = "user@example.com"
        },
        new SmsContent
        {
            message = "Your code is 123456",
            phoneNumber = "+1234567890"
        },
        new PushContent
        {
            title = "New Message",
            message = "You have a new message waiting.",
            deviceToken = "abcdef123456"
        }
    ];
}
