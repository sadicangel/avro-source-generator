using AvroSourceGenerator.IntegrationTests.Schemas;
using Chr.Avro.Representation;
using Chr.Avro.Serialization;

namespace AvroSourceGenerator.IntegrationTests.Chr;

public sealed class UnionSerializationTests
{
    [Theory]
    [InlineData("email")]
    [InlineData("sms")]
    [InlineData("push")]
    public void Interface_variants_survive_binary_serialization(string kind)
    {
        INotificationContentVariant content = kind switch
        {
            "email" => new EmailContent { subject = "Subject", body = "Body", recipientEmail = "user@example.com" },
            "sms" => new SmsContent { message = "Message", phoneNumber = "+1234567890" },
            "push" => new PushContent { title = "Title", message = "Message", deviceToken = "device" },
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        var expected = new Notification { content = content };
        var schema = new JsonSchemaReader().Read(File.ReadAllText(Path.Combine("Schemas", "Notification.avsc")));
        var serialize = new BinarySerializerBuilder(BinarySerializerBuilder.CreateDefaultCaseBuilders()
            .Prepend(builder => new NotificationContentVariantSerializerBuilderCase(builder)))
            .BuildDelegate<Notification>(schema);
        var deserialize = new BinaryDeserializerBuilder(BinaryDeserializerBuilder.CreateDefaultCaseBuilders()
            .Prepend(builder => new NotificationContentVariantDeserializerBuilderCase(builder)))
            .BuildDelegate<Notification>(schema);
        using var stream = new MemoryStream();
        serialize(expected, new global::Chr.Avro.Serialization.BinaryWriter(stream));
        var reader = new global::Chr.Avro.Serialization.BinaryReader(stream.ToArray());
        var actual = deserialize(ref reader);

        Assert.Equal(content.GetType(), actual.content.GetType());
        Assert.Equal(expected, actual);
        Assert.Equal(stream.Length, reader.Index);
    }
}
