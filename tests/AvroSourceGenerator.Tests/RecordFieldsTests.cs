using System.Text.Json.Nodes;
using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class RecordFieldsTests
{
    [Theory]
    [MemberData(nameof(InvalidFields))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("record").With("fields", JsonNode.Parse(json)!).ToString();

        return TestHelper.VerifyDiagnostic(schema);
    }

    public static TheoryData<string> InvalidFields() => new(
        ["null", "{}"]);
}
