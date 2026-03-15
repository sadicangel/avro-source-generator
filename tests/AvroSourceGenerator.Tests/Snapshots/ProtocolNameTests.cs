namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class ProtocolNameTests
{
    [Theory]
    [MemberData(nameof(ValidNames))]
    public Task Verify(string name)
    {
        var schema = TestSchemas.Get("protocol").With("protocol", name).ToString();

        return Snapshot.Schema(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("protocol").With("protocol", JsonNode.Parse(json)!).ToString();

        return Snapshot.Diagnostic(schema);
    }

    public static TheoryData<string> ValidNames() => new TheoryData<string>("PascalCase", "snake_case", "object");

    public static TheoryData<string> InvalidNames() => new TheoryData<string>("null", "\"\"", "[]");
}
