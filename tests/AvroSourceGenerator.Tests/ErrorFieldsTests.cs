using System.Text.Json.Nodes;
using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class ErrorFieldsTests
{
    [Theory]
    [MemberData(nameof(InvalidFields))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("error").With("fields", JsonNode.Parse(json)!).ToString();

        return TestHelper.VerifyDiagnostic(schema);
    }

    public static TheoryData<string> InvalidFields() => new(
        ["null", "{}"]);
}
