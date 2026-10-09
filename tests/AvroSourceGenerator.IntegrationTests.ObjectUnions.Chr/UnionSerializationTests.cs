using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

public sealed partial class UnionSerializationTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("string")]
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

    [Theory]
    [InlineData("int")]
    [InlineData("fixed")]
    [InlineData("record")]
    public void Default_chr_builder_cannot_select_non_string_object_union_members(string kind)
    {
        var expected = SerializationCases.CreatePayment(kind);
        Assert.Throws<global::Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(() => BinarySerialization.Roundtrip(expected));
    }

    [Fact]
    public void Default_chr_builder_serializes_object_union_enum_as_string()
    {
        var expected = SerializationCases.CreatePayment("enum");
        var actual = BinarySerialization.Roundtrip(expected);

        Assert.IsType<Schemas.DeferredPaymentReason>(expected.paymentMethod);
        Assert.Equal(nameof(Schemas.DeferredPaymentReason.PendingApproval), Assert.IsType<string>(actual.paymentMethod));
    }
}
