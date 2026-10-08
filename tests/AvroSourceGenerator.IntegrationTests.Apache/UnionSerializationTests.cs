using Avro.IO;
using Avro.Specific;
using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Apache;

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
        using var stream = new MemoryStream();
        new SpecificDatumWriter<Notification>(Notification._SCHEMA).Write(expected, new BinaryEncoder(stream));
        stream.Position = 0;
        var actual = new SpecificDatumReader<Notification>(Notification._SCHEMA, Notification._SCHEMA)
            .Read(null!, new BinaryDecoder(stream));

        Assert.Equal(content.GetType(), actual.content.GetType());
        Assert.Equal(expected, actual);
        Assert.Equal(stream.Length, stream.Position);
    }
}
