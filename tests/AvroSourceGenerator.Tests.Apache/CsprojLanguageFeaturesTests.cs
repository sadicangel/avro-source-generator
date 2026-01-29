namespace AvroSourceGenerator.Tests.Apache;

public sealed class CsprojLanguageFeaturesTests
{
    [Theory]
    [MemberData(nameof(LanguageFeaturesSchemaPairs))]
    public Task Verify(string languageFeatures, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With(
            "fields",
            [
                new
                {
                    type = "string",
                    name = "Field"
                }
            ]).ToString();

        var config = new ProjectConfig { LanguageFeatures = languageFeatures };

        return VerifySourceCode(schema, null, config);
    }

    public static MatrixTheoryData<string, string> LanguageFeaturesSchemaPairs() => new MatrixTheoryData<string, string>([.. Enum.GetNames(typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.LanguageFeatures", throwOnError: true)!).Where(n => n.StartsWith("CSharp")), "invalid"], ["enum", "error", "fixed", "record", "protocol"]);
}
