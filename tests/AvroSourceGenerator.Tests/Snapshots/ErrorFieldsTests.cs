namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class ErrorFieldsTests
{
    [Theory]
    [MemberData(nameof(InvalidFields))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("error").With("fields", JsonNode.Parse(json)!).ToString();

        return Snapshot.Diagnostic(schema);
    }

    public static TheoryData<string> InvalidFields() => new TheoryData<string>("null", "{}");
}
