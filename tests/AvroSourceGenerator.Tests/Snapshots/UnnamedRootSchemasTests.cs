namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class UnnamedRootSchemasTests
{
    [Theory]
    [MemberData(nameof(ValidUnnamedRootSchemas))]
    public Task Verify(string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        return Snapshot.Schema(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidUnnamedRootSchemas))]
    public Task Diagnostic(string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        return Snapshot.Diagnostic(schema);
    }

    public static TheoryData<string> ValidUnnamedRootSchemas() => new TheoryData<string>("array<record>", "map<record>", "[null, record]");

    public static TheoryData<string> InvalidUnnamedRootSchemas() => new TheoryData<string>("array<string>", "map<string>", "[null, string]");
}
