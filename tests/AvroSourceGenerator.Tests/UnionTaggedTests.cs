using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class UnionTaggedTests
{
    [Fact]
    public Task Verify()
    {
        var schema = """
            {
              "type": "record",
              "name": "Notification",
              "namespace": "com.example.notifications",
              "fields": [
                {
                  "name": "type",
                  "type": {
                    "type": "enum",
                    "name": "NotificationType",
                    "symbols": ["EMAIL", "SMS", "PUSH"]
                  },
                  "doc": "Indicates which type of content is stored in the 'content' field"
                },
                {
                  "name": "content",
                  "type": [
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
                  ],
                  "doc": "The type-specific notification payload"
                }
              ]
            }
            """;

        return TestHelper.VerifySourceCode(schema);
    }
}
