using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class SerializationCases
{
    private static readonly Guid s_id = new Guid("01234567-89ab-cdef-0123-456789abcdef");
    private static readonly DateTime s_timestamp = new DateTime(2026, 1, 2, 3, 4, 5, 123, DateTimeKind.Utc);

    public static Primitives CreatePrimitives()
    {
        var expected = new Primitives
        {
            intField = 42,
            longField = 42L,
            floatField = 42.42f,
            doubleField = 42.42,
            boolField = true,
            stringField = "Hello, Avro!",
            bytesField = "Hello, Avro!"u8.ToArray(),
            nullableInt1 = 42,
            nullableLong1 = 42L,
            nullableFloat1 = 42.42f,
            nullableDouble1 = 42.42,
            nullableBool1 = true,
            nullableString1 = "Hello, Avro!",
            nullableBytes1 = "Hello, Avro!"u8.ToArray(),
            nullableInt2 = null,
            nullableLong2 = null,
            nullableFloat2 = null,
            nullableDouble2 = null,
            nullableBool2 = null,
            nullableString2 = null,
            nullableBytes2 = null,
        };
        return expected;
    }

    public static Collections CreateCollections()
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
        return expected;
    }

    public static Enums CreateEnums() => new Enums
    {
        status = Status.ACTIVE,
        nullableStatus1 = Status.INACTIVE,
        nullableStatus2 = null,
    };

    public static JavaExtensions CreateJavaExtensions()
    {
        var expected = new JavaExtensions
        {
            default_class = "default-string",
            string_class = "java-string",
            stringable_class = "12345.67",
            default_map = new Dictionary<string, int>
            {
                ["one"] = 1,
                ["two"] = 2
            },
            string_map = new Dictionary<string, int>
            {
                ["alpha"] = 10,
                ["beta"] = 20
            },
            stringable_map = new Dictionary<string, int>
            {
                ["1.5"] = 100,
                ["2.75"] = 200
            }
        };
        return expected;
    }

    public static ImportedEnvelope CreateImports() => new ImportedEnvelope
    {
        Item = new NestedType { displayName = "Imported" },
    };

    public static Notification CreateNotification(string kind) => kind switch
    {
        "email" => new Notification
        {
            content = new EmailContent { subject = "Subject", body = "Body", recipientEmail = "user@example.com" },
        },
        "sms" => new Notification
        {
            content = new SmsContent { message = "Message", phoneNumber = "+1234567890" },
        },
        "push" => new Notification
        {
            content = new PushContent { title = "Title", message = "Message", deviceToken = "device" },
        },
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static PaymentRecord CreatePayment(string kind) => kind switch
    {
        "null" => new PaymentRecord { paymentMethod = null },
        "string" => new PaymentRecord { paymentMethod = "cash" },
        "int" => new PaymentRecord { paymentMethod = 2 },
        "fixed" => CreateFixedPayment(),
        "record" => new PaymentRecord
        {
            paymentMethod = new CreditCardPayment
            {
                cardNumber = "4111111111111111",
                cardHolder = "Alice Example",
                expirationDate = "12/26",
            },
        },
        "enum" => new PaymentRecord { paymentMethod = DeferredPaymentReason.PendingApproval },
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

}
