using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

public sealed partial class UnionSerializationTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("fixed")]
    [InlineData("record")]
    [InlineData("enum", Skip = "Apache.Avro returns enum ordinals in unions; awaiting Apache Avro PR #4031.")]
    public void Mixed_union_variants_survive_binary_serialization(string kind) => VerifyPayment(kind);

    private static void VerifyNotificationRepresentation(Notification actual)
    {
        Assert.True(typeof(Notification).GetProperty(nameof(Notification.content))!.PropertyType.IsInterface);
        Assert.IsAssignableFrom<INotificationContentVariant>(actual.content);
    }

    private static void VerifyPaymentRepresentation(PaymentRecord actual)
    {
        Assert.Equal(typeof(object), typeof(PaymentRecord).GetProperty(nameof(PaymentRecord.paymentMethod))!.PropertyType);
    }
}
