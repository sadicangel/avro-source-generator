using AvroSourceGenerator.Configuration;

namespace AvroSourceGenerator.Tests;

public sealed class CsprojLanguageFeaturesTests
{
    [Theory]
    [MemberData(nameof(LanguageFeaturesSchemaPairs))]
    public Task Verify(string languageFeatures, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("fields", [new { type = "string", name = "Field" }]).ToString();

        var config = new ProjectConfig() with { LanguageFeatures = languageFeatures };

        return VerifySourceCode(schema, default, config);
    }

    public static MatrixTheoryData<string, string> LanguageFeaturesSchemaPairs() => new(
        [.. Enum.GetNames(typeof(AvroSourceGenerator).Assembly.GetType("AvroSourceGenerator.Configuration.LanguageFeatures", throwOnError: true)!).Where(n => n.StartsWith("CSharp")), "invalid"],
        ["enum", "error", "fixed", "record", "protocol"]);
}
