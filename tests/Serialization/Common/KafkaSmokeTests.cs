using AvroSourceGenerator.IntegrationTests.Schemas;
using AvroSourceGenerator.SmokeTests;
using Xunit;

namespace AvroSourceGenerator.IntegrationTests;

public sealed partial class KafkaSmokeTests(DockerFixture docker)
{
    [Fact]
    public async Task Composite_message_survives_kafka_and_schema_registry()
    {
        var expected = new KafkaSmoke
        {
            primitives = SerializationCases.CreatePrimitives(),
            collections = SerializationCases.CreateCollections(),
            enums = SerializationCases.CreateEnums(),
            logicalTypes = SerializationCases.CreateLogicalTypes(),
            transaction = SerializationCases.CreateRecords(),
            imported = SerializationCases.CreateImports(),
            javaExtensions = SerializationCases.CreateJavaExtensions(),
            email = SerializationCases.CreateNotification("email"),
            sms = SerializationCases.CreateNotification("sms"),
            push = SerializationCases.CreateNotification("push"),
            nullPayment = SerializationCases.CreatePayment("null"),
            stringPayment = SerializationCases.CreatePayment("string"),
            intPayment = SerializationCases.CreatePayment("int"),
            fixedPayment = SerializationCases.CreatePayment("fixed"),
            recordPayment = SerializationCases.CreatePayment("record"),
        };
        var actual = await RoundtripAsync(docker, expected, TestContext.Current.CancellationToken);
        SerializationAssertions.Equal(expected.primitives, actual.primitives);
        SerializationAssertions.Equal(expected.collections, actual.collections);
        SerializationAssertions.Equal(expected.enums, actual.enums);
        SerializationAssertions.Equal(expected.logicalTypes, actual.logicalTypes);
        SerializationAssertions.Equal(expected.transaction, actual.transaction);
        SerializationAssertions.Equal(expected.imported, actual.imported);
        SerializationAssertions.Equal(expected.javaExtensions, actual.javaExtensions);
        VerifyNotification(expected.email, actual.email);
        VerifyNotification(expected.sms, actual.sms);
        VerifyNotification(expected.push, actual.push);
        VerifyPayment(expected.nullPayment, actual.nullPayment);
        VerifyPayment(expected.stringPayment, actual.stringPayment);
        VerifyPayment(expected.intPayment, actual.intPayment);
        VerifyPayment(expected.fixedPayment, actual.fixedPayment);
        VerifyPayment(expected.recordPayment, actual.recordPayment);
    }

    private static void VerifyNotification(Notification expected, Notification actual)
    {
        Assert.Equal(SerializationCases.NotificationValue(expected)?.GetType(), SerializationCases.NotificationValue(actual)?.GetType());
        SerializationAssertions.Equal(expected, actual);
    }

    private static void VerifyPayment(PaymentRecord expected, PaymentRecord actual)
    {
        Assert.Equal(SerializationCases.PaymentValue(expected)?.GetType(), SerializationCases.PaymentValue(actual)?.GetType());
        SerializationAssertions.Equal(expected, actual);
    }
}
