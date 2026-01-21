namespace AvroSourceGenerator.Tests.Apache;

public sealed class FixedNameTests
{
    [Theory]
    [MemberData(nameof(ValidNameSchemaPairs))]
    public Task Verify(string name)
    {
        var schema = TestSchemas.Get("fixed").With("name", name).ToString();

        return VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidNameSchemaPairs))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("fixed").With("name", JsonNode.Parse(json)!).ToString();

        return VerifyDiagnostic(schema);
    }

    public static TheoryData<string> ValidNameSchemaPairs() => new("PascalCase", "snake_case", "object");

    public static TheoryData<string> InvalidNameSchemaPairs() => new("null", "\"\"", "[]");
}
