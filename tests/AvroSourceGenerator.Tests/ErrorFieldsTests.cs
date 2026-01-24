namespace AvroSourceGenerator.Tests;

public sealed class ErrorFieldsTests
{
    [Theory]
    [MemberData(nameof(InvalidFields))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("error").With("fields", JsonNode.Parse(json)!).ToString();

        return VerifyDiagnostic(schema);
    }

    public static TheoryData<string> InvalidFields() => new("null", "{}");
}
