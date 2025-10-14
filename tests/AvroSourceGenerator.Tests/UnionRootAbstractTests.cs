namespace AvroSourceGenerator.Tests;

public sealed class UnionRootAbstractTests
{
    [Fact]
    public Task Verify()
    {
        var schema = """
        [
            "null",
            {
                "type": "record",
                "name": "EmailContent",
                "fields": [
                    { "name": "subject", "type": "string" },
                    { "name": "body", "type": "string" },
                    { "name": "recipientEmail", "type": "string" }
                ]
            },
            {
                "type": "record",
                "name": "SmsContent",
                "fields": [
                    { "name": "message", "type": "string" },
                    { "name": "phoneNumber", "type": "string" }
                ]
            },
            {
                "type": "record",
                "name": "PushContent",
                "fields": [
                    { "name": "title", "type": "string" },
                    { "name": "message", "type": "string" },
                    { "name": "deviceToken", "type": "string" }
                ]
            }
        ]
        """;

        return VerifySourceCode(schema);
    }
}
