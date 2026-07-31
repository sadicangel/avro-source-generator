namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class NamespaceTests
{
    [Theory]
    [MemberData(nameof(ValidNamespaceSchemaPairs))]
    public Task Verify(string @namespace, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("namespace", @namespace).ToString();

        return Snapshot.Schema(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidNamespaceSchemaPairs))]
    public Task Diagnostic(string json, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("namespace", JsonNode.Parse(json)!).ToString();

        return Snapshot.Diagnostic(ProjectFile.Schema(schema));
    }

    public static MatrixTheoryData<string, string> ValidNamespaceSchemaPairs() => new([null!, "", "PascalCase.snake_case.object"], ["enum", "error", "record", "protocol"]);

    public static MatrixTheoryData<string, string> InvalidNamespaceSchemaPairs() => new(["[]"], ["enum", "error", "record", "protocol"]);
}
