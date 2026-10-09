using AvroSourceGenerator.IntegrationTests.Schemas;
using Chr.Avro.Serialization;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class BinarySerialization
{
    public static BinarySerializerBuilder CreateSerializerBuilder() => new BinarySerializerBuilder(
        BinarySerializerBuilder.CreateDefaultCaseBuilders()
            .Prepend(builder => new NotificationContentVariantSerializerBuilderCase(builder)));

    public static BinaryDeserializerBuilder CreateDeserializerBuilder() => new BinaryDeserializerBuilder(
        BinaryDeserializerBuilder.CreateDefaultCaseBuilders()
            .Prepend(builder => new NotificationContentVariantDeserializerBuilderCase(builder)));
}
