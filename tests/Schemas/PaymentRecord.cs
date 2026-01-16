using System.Runtime.CompilerServices;
using Xunit.Sdk;

#pragma warning disable IDE0130
namespace AvroSourceGenerator.IntegrationTests.Schemas;

partial record PaymentRecord;

partial record CreditCardPayment : IXunitSerializable
{
    public void Deserialize(IXunitSerializationInfo info)
    {
        SetCardNumber(this, (string)info.GetValue(nameof(cardNumber))!);
        SetCardHolder(this, (string)info.GetValue(nameof(cardHolder))!);
        SetExpirationDate(this, (string)info.GetValue(nameof(expirationDate))!);

        return;

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_cardHolder")]
        static extern void SetCardHolder(CreditCardPayment obj, string value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_cardNumber")]
        static extern void SetCardNumber(CreditCardPayment obj, string value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_expirationDate")]
        static extern void SetExpirationDate(CreditCardPayment obj, string value);
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(cardNumber), cardNumber);
        info.AddValue(nameof(cardHolder), cardHolder);
        info.AddValue(nameof(expirationDate), expirationDate);
    }
}
