namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class DocumentationTests
{
    [Theory]
    [MemberData(nameof(ValidDocumentationSchemaPairs))]
    public Task Verify(string doc, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("doc", doc).ToString();

        return Snapshot.Schema(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidDocumentationSchemaPairs))]
    public Task Diagnostic(string json, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("doc", JsonNode.Parse(json)!).ToString();

        return Snapshot.Diagnostic(schema);
    }

    public static MatrixTheoryData<string, string> ValidDocumentationSchemaPairs() => new MatrixTheoryData<string, string>([null!, "", "Single line comment", "Multi\nline\ncomment"], ["enum", "error", "record", "protocol"]);

    public static MatrixTheoryData<string, string> InvalidDocumentationSchemaPairs() => new MatrixTheoryData<string, string>(["[]"], ["enum", "error", "record", "protocol"]);
}
