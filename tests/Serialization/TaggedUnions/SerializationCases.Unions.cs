using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class SerializationCases
{
    public static object? NotificationValue(Notification record) => record.content.Value;

    public static object? PaymentValue(PaymentRecord record) => record.paymentMethod?.Value;
}
