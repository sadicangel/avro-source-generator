using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class CsprojLanguageFeaturesTests
{
    [Theory]
    [MemberData(nameof(LanguageFeaturesSchemaPairs))]
    public Task Verify(string languageFeatures, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("fields", [new { type = "string", name = "Field" }]).ToString();

        var config = ProjectConfig.Default with
        {
            GlobalOptions = new Dictionary<string, string>
            {
                ["AvroSourceGeneratorLanguageFeatures"] = languageFeatures
            }
        };

        return TestHelper.VerifySourceCode(schema, default, config);
    }

    public static MatrixTheoryData<string, string> LanguageFeaturesSchemaPairs() => new(
        [.. Enum.GetNames<LanguageFeatures>().Where(n => n.StartsWith("CSharp")), "invalid"],
        ["enum", "error", "fixed", "record", "protocol"]);
}
