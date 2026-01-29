namespace AvroSourceGenerator.Tests.Chr;

public sealed class FixedDocumentationTests
{
    [Theory]
    [MemberData(nameof(ValidDocumentationSchemaPairs))]
    public Task Verify(string doc)
    {
        var schema = TestSchemas.Get("record")
            .With(
                "fields",
                [
                    new JsonObject
                    {
                        ["name"] = "fixedField",
                        ["type"] = TestSchemas.Get("fixed").With("doc", doc)
                    }
                ]).ToString();

        return VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidDocumentationSchemaPairs))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("record")
            .With(
                "fields",
                [
                    new JsonObject
                    {
                        ["name"] = "fixedField",
                        ["type"] = TestSchemas.Get("fixed").With("doc", JsonNode.Parse(json)!)
                    }
                ]).ToString();

        return VerifyDiagnostic(schema);
    }

    public static TheoryData<string> ValidDocumentationSchemaPairs() => new(null!, "", "Single line comment", "Multi\nline\ncomment");

    public static TheoryData<string> InvalidDocumentationSchemaPairs() => new("[]");
}
