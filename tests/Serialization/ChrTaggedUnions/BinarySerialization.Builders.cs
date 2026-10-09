using AvroSourceGenerator.IntegrationTests.Schemas;
using Chr.Avro.Serialization;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class BinarySerialization
{
    public static BinarySerializerBuilder CreateSerializerBuilder() => new BinarySerializerBuilder(
        BinarySerializerBuilder.CreateDefaultCaseBuilders()
            .Prepend(builder => new NotificationContentUnionSerializerBuilderCase(builder))
            .Prepend(builder => new PaymentRecordPaymentMethodUnionSerializerBuilderCase(builder)));

    public static BinaryDeserializerBuilder CreateDeserializerBuilder() => new BinaryDeserializerBuilder(
        BinaryDeserializerBuilder.CreateDefaultCaseBuilders()
            .Prepend(builder => new NotificationContentUnionDeserializerBuilderCase(builder))
            .Prepend(builder => new PaymentRecordPaymentMethodUnionDeserializerBuilderCase(builder)));
}
