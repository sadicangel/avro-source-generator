namespace AvroSourceGenerator.Tests.Apache.Snapshots;

public sealed class KeywordFieldNameTests
{
    [Theory]
    [InlineData("CSharp11")]
    [InlineData("CSharp12")]
    public Task Verify(string languageFeatures)
    {
        const string Schema = """
            {
                "type": "record",
                "name": "KeywordFields",
                "namespace": "SchemaNamespace",
                "fields": [
                    {
                        "type": "string",
                        "name": "int"
                    }
                ]
            }
            """;

        return Snapshot.Schema(
            Schema,
            config => config with { LanguageFeatures = languageFeatures });
    }
}
