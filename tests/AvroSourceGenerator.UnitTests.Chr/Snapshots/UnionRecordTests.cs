using AvroSourceGenerator.Compiler;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Chr.Snapshots;

public sealed class UnionRecordTests
{
    [Theory]
    [InlineData(nameof(LanguageFeatures.CSharp14))]
    [InlineData(nameof(LanguageFeatures.CSharp15))]
    public Task Verify(string languageFeatures)
    {
        return Snapshot.Schema(
            """
            {
                "type": "record",
                "name": "Envelope",
                "fields": [
                    {
                        "name": "content",
                        "type": [
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
                    }
                ]
            }
            """,
            config => config with
            {
                LanguageFeatures = languageFeatures,
                PreviewFeatures = languageFeatures == nameof(LanguageFeatures.CSharp15) ? "Unions" : "None",
                LanguageVersion = languageFeatures == nameof(LanguageFeatures.CSharp15) ? LanguageVersion.Preview : LanguageVersion.CSharp14
            }).UseParameters(languageFeatures);
    }
}
