using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Chr.RoundtripTests;

public class UnionObjectsTests(DockerFixture dockerFixture)
{
    [Theory]
    [InlineData(null), InlineData(2), InlineData("cash"), MemberData(nameof(CreditCardPaymentVariant))]
    public async Task Union_types_mapped_to_object_remain_unchanged_after_roundtrip_to_kafka(object? variant)
    {
        Assert.Skip("Union types mapped to object are not supported out of the box.");

        var expected = new PaymentRecord { paymentMethod = variant };

        var actual = await dockerFixture.RoundtripAsync(expected, PaymentRecord.GetSchema(), TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }

    public static TheoryData<CreditCardPayment> CreditCardPaymentVariant() =>
    [
        new CreditCardPayment
        {
            cardNumber = "4111111111111111",
            cardHolder = "Alice Example",
            expirationDate = "12/26",
        }
    ];
}
