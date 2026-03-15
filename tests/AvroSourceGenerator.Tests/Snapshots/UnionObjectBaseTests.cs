namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class UnionObjectBaseTests
{
    [Fact]
    public Task Verify()
    {
        return Snapshot.Schema(
            """
            {
              "type": "record",
              "name": "Notification",
              "namespace": "com.example.notifications",
              "fields": [
                {
                  "name": "content",
                  "type": [
                    "string",
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
            """);
    }
}
