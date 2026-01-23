using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Apache.RoundtripTests;

public class UnionInterfacesTests(DockerFixture dockerFixture)
{
    [Theory]
    [MemberData(nameof(NotificationVariants))]
    public async Task Union_types_mapped_to_abstract_remain_unchanged_after_roundtrip_to_kafka(INotificationContentVariant content)
    {
        var expected = new Notification { content = content };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }

    public static TheoryData<INotificationContentVariant> NotificationVariants() =>
    [
        new EmailContent
        {
            subject = "Welcome!",
            body = "Thanks for signing up.",
            recipientEmail = "user@example.com"
        },
        new SmsContent
        {
            message = "Your code is 123456",
            phoneNumber = "+1234567890"
        },
        new PushContent
        {
            title = "New Message",
            message = "You have a new message waiting.",
            deviceToken = "abcdef123456"
        }
    ];
}
