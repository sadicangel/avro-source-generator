using System.Text.Json.Nodes;
using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class NamespaceTests
{
    [Theory]
    [MemberData(nameof(ValidNamespaceSchemaPairs))]
    public Task Verify(string @namespace, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("namespace", @namespace).ToString();

        return TestHelper.VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidNamespaceSchemaPairs))]
    public Task Diagnostic(string json, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("namespace", JsonNode.Parse(json)!).ToString();

        return TestHelper.VerifyDiagnostic(schema);
    }

    public static MatrixTheoryData<string, string> ValidNamespaceSchemaPairs() => new(
        [null!, "", "PascalCase.snake_case.object"],
        ["enum", "error", "fixed", "record", "protocol"]);

    public static MatrixTheoryData<string, string> InvalidNamespaceSchemaPairs() => new(
        ["[]"],
        ["enum", "error", "fixed", "record", "protocol"]);
}
