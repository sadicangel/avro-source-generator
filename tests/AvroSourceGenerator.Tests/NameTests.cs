using System.Text.Json.Nodes;
using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class NameTests
{
    [Theory]
    [MemberData(nameof(ValidNameSchemaPairs))]
    public Task Verify(string name, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("name", name).ToString();

        return TestHelper.VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidNameSchemaPairs))]
    public Task Diagnostic(string json, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("name", JsonNode.Parse(json)!).ToString();

        return TestHelper.VerifyDiagnostic(schema);
    }

    public static MatrixTheoryData<string, string> ValidNameSchemaPairs() => new(
        ["PascalCase", "snake_case", "object"],
        ["enum", "error", "fixed", "record"]);

    public static MatrixTheoryData<string, string> InvalidNameSchemaPairs() => new(
        ["null", "\"\"", "[]"],
        ["enum", "error", "fixed", "record"]);
}
