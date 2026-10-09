using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class SerializationCases
{
    public static object? NotificationValue(Notification record) => record.content;

    public static object? PaymentValue(PaymentRecord record) => record.paymentMethod;
}
