namespace AvroSourceGenerator.Tests;

public sealed class ProtocolNameTests
{
    [Theory]
    [MemberData(nameof(ValidNames))]
    public Task Verify(string name)
    {
        var schema = TestSchemas.Get("protocol").With("protocol", name).ToString();

        return VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("protocol").With("protocol", JsonNode.Parse(json)!).ToString();

        return VerifyDiagnostic(schema);
    }

    public static TheoryData<string> ValidNames() => new(["PascalCase", "snake_case", "object"]);

    public static TheoryData<string> InvalidNames() => new(["null", "\"\"", "[]"]);
}
