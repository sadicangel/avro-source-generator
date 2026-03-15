namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class CsprojLanguageFeaturesTests
{
    [Theory]
    [MemberData(nameof(LanguageFeaturesSchemaPairs))]
    public Task Verify(string languageFeatures, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType)
            .With(
                "fields",
                [
                    new
                    {
                        type = "string",
                        name = "Field",
                    }
                ])
            .With(
                "types",
                [
                    new
                    {
                        name = "Greeting",
                        type = "record",
                        fields = new[]
                        {
                            new
                            {
                                type = "string",
                                name = "message",
                            }
                        }
                    },
                    new
                    {
                        name = "Curse",
                        type = "error",
                        fields = new[]
                        {
                            new
                            {
                                type = "string",
                                name = "message",
                            }
                        }
                    },
                ])
            .With(
                "messages",
                new
                {
                    hello = new
                    {
                        doc = "Say hello.",
                        request = new[]
                        {
                            new
                            {
                                type = "Greeting",
                                name = "greeting",
                            }
                        },
                        response = "Greeting",
                        errors = new[] { "Curse" }
                    }
                }).ToString();

        return Snapshot.Schema(schema, config => config with { LanguageFeatures = languageFeatures });
    }

    public static MatrixTheoryData<string, string> LanguageFeaturesSchemaPairs() => new MatrixTheoryData<string, string>([.. Enum.GetNames(typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.LanguageFeatures", throwOnError: true)!).Where(n => n.StartsWith("CSharp")), "invalid"], ["enum", "error", "record", "protocol"]);
}
