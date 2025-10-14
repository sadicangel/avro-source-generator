namespace AvroSourceGenerator.Tests;

public sealed class RecordFieldsTests
{
    [Theory]
    [MemberData(nameof(InvalidFields))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("record").With("fields", JsonNode.Parse(json)!).ToString();

        return VerifyDiagnostic(schema);
    }

    public static TheoryData<string> InvalidFields() => new(
        ["null", "{}"]);
}
