using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.RoundtripTests.Apache;

public class UnionMixedTypesTests(DockerFixture dockerFixture)
{
    [Theory]
    [MemberData(nameof(PaymentMethodVariants))]
    public async Task Native_mixed_unions_remain_unchanged_after_roundtrip_to_kafka(PaymentRecordPaymentMethodUnion? variant)
    {
        if (variant?.Value is DeferredPaymentReason)
            Assert.Skip("Apache.Avro returns enum ordinals in unions; awaiting https://github.com/apache/avro/pull/4031.");

        var expected = new PaymentRecord { paymentMethod = variant };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        Assert.Equal(expected.paymentMethod?.Value?.GetType(), actual.paymentMethod?.Value?.GetType());
        Assert.EqualAsJson(expected, actual);
    }

    public static TheoryData<PaymentRecordPaymentMethodUnion?> PaymentMethodVariants() =>
    [
        default(PaymentRecordPaymentMethodUnion?),
        new PaymentRecordPaymentMethodUnion(2),
        new PaymentRecordPaymentMethodUnion(new PaymentMethodId { Value = Guid.NewGuid().ToByteArray() }),
        new PaymentRecordPaymentMethodUnion("cash"),
        new PaymentRecordPaymentMethodUnion(
            new CreditCardPayment
            {
                cardNumber = "4111111111111111",
                cardHolder = "Alice Example",
                expirationDate = "12/26",
            }),
        new PaymentRecordPaymentMethodUnion(DeferredPaymentReason.PendingApproval)
    ];
}
