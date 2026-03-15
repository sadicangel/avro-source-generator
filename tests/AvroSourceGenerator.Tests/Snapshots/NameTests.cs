namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class NameTests
{
    [Theory]
    [MemberData(nameof(ValidNameSchemaPairs))]
    public Task Verify(string name, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("name", name).ToString();

        return Snapshot.Schema(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidNameSchemaPairs))]
    public Task Diagnostic(string json, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("name", JsonNode.Parse(json)!).ToString();

        return Snapshot.Diagnostic(schema);
    }

    public static MatrixTheoryData<string, string> ValidNameSchemaPairs() => new MatrixTheoryData<string, string>(["PascalCase", "snake_case", "object"], ["enum", "error", "record"]);

    public static MatrixTheoryData<string, string> InvalidNameSchemaPairs() => new MatrixTheoryData<string, string>(["null", "\"\"", "[]"], ["enum", "error", "record"]);
}
