using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.RoundtripTests.Chr;

public class UnionRecordsTests(DockerFixture dockerFixture)
{
    [Theory]
    [MemberData(nameof(NotificationVariants))]
    public async Task Native_record_unions_remain_unchanged_after_roundtrip_to_kafka(
        NotificationContentUnion content)
    {
        var expected = new Notification { content = content };

        var actual = await dockerFixture.RoundtripAsync(expected, Notification.GetSchema(), TestContext.Current.CancellationToken);

        Assert.Equal(expected.content.Value?.GetType(), actual.content.Value?.GetType());
        Assert.EqualAsJson(expected, actual);
    }

    public static TheoryData<NotificationContentUnion> NotificationVariants() =>
    [
        new NotificationContentUnion(
            new EmailContent
            {
                subject = "Welcome!",
                body = "Thanks for signing up.",
                recipientEmail = "user@example.com"
            }),
        new NotificationContentUnion(
            new SmsContent
            {
                message = "Your code is 123456",
                phoneNumber = "+1234567890"
            }),
        new NotificationContentUnion(
            new PushContent
            {
                title = "New Message",
                message = "You have a new message waiting.",
                deviceToken = "abcdef123456"
            })
    ];
}
