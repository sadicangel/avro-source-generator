using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace AvroSourceGenerator.IntegrationTests.Schemas;

partial record PaymentRecord { }

partial record CreditCardPayment : IXunitSerializable
{
    public void Deserialize(IXunitSerializationInfo info)
    {
        Set_cardNumber(this, (string)(info.GetValue(nameof(cardNumber))!));
        Set_cardHolder(this, (string)(info.GetValue(nameof(cardHolder))!));
        Set_expirationDate(this, (string)(info.GetValue(nameof(expirationDate))!));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_cardHolder")]
        extern static void Set_cardHolder(CreditCardPayment obj, string value);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_cardNumber")]
        extern static void Set_cardNumber(CreditCardPayment obj, string value);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_expirationDate")]
        extern static void Set_expirationDate(CreditCardPayment obj, string value);
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(cardNumber), cardNumber);
        info.AddValue(nameof(cardHolder), cardHolder);
        info.AddValue(nameof(expirationDate), expirationDate);
    }
}
