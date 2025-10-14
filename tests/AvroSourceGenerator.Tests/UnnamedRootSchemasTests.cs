namespace AvroSourceGenerator.Tests;

public sealed class UnnamedRootSchemasTests
{
    [Theory]
    [MemberData(nameof(ValidUnnamedRootSchemas))]
    public Task Verify(string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        return VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidUnnamedRootSchemas))]
    public Task Diagnostic(string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        return VerifyDiagnostic(schema);
    }

    public static TheoryData<string> ValidUnnamedRootSchemas() => new(
        ["array<record>", "map<record>", "[null, record]"]);

    public static TheoryData<string> InvalidUnnamedRootSchemas() => new(
        ["array<string>", "map<string>", "[null, string]"]);
}
