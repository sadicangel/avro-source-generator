namespace AvroSourceGenerator.Tests;

public sealed class DocumentationTests
{
    [Theory]
    [MemberData(nameof(ValidDocumentationSchemaPairs))]
    public Task Verify(string doc, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("doc", doc).ToString();

        return VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidDocumentationSchemaPairs))]
    public Task Diagnostic(string json, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("doc", JsonNode.Parse(json)!).ToString();

        return VerifyDiagnostic(schema);
    }

    public static MatrixTheoryData<string, string> ValidDocumentationSchemaPairs() => new(
        [null!, "", "Single line comment", "Multi\nline\ncomment"],
        ["enum", "error", "fixed", "record", "protocol"]);

    public static MatrixTheoryData<string, string> InvalidDocumentationSchemaPairs() => new(
        ["[]"],
        ["enum", "error", "fixed", "record", "protocol"]);
}
