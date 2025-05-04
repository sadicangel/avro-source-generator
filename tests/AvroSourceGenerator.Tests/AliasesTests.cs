using System.Text.Json.Nodes;
using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class AliasesTests
{
    [Theory]
    [MemberData(nameof(ValidAliasesSchemaPairs))]
    public Task Verify(string[] aliases, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("aliases", aliases).ToString();

        return TestHelper.VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidAliasesSchemaPairs))]
    public Task Diagnostic(string json, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("aliases", JsonNode.Parse(json)!).ToString();

        return TestHelper.VerifyDiagnostic(schema);
    }

    public static MatrixTheoryData<string[], string> ValidAliasesSchemaPairs() => new(
        [null!, [], ["Alias1", "Alias2"]],
        ["enum", "error", "fixed", "record"]);

    public static MatrixTheoryData<string, string> InvalidAliasesSchemaPairs() => new(
        ["{}"],
        ["enum", "error", "fixed", "record"]);
}
