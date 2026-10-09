using Xunit;

namespace AvroSourceGenerator.IntegrationTests;

public sealed partial class UnionSerializationTests
{
    [Theory]
    [InlineData("email")]
    [InlineData("sms")]
    [InlineData("push")]
    public void Record_union_variants_survive_binary_serialization(string kind)
    {
        var expected = SerializationCases.CreateNotification(kind);
        var actual = BinarySerialization.Roundtrip(expected);

        VerifyNotificationRepresentation(actual);
        Assert.Equal(SerializationCases.NotificationValue(expected)?.GetType(), SerializationCases.NotificationValue(actual)?.GetType());
        SerializationAssertions.Equal(expected, actual);
    }

    private static void VerifyPayment(string kind)
    {
        var expected = SerializationCases.CreatePayment(kind);
        var actual = BinarySerialization.Roundtrip(expected);

        VerifyPaymentRepresentation(actual);
        Assert.Equal(SerializationCases.PaymentValue(expected)?.GetType(), SerializationCases.PaymentValue(actual)?.GetType());
        SerializationAssertions.Equal(expected, actual);
    }
}
