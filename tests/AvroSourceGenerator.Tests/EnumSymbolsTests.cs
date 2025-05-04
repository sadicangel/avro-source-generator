using System.Text.Json.Nodes;
using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public class EnumSymbolsTests
{
    [Theory]
    [MemberData(nameof(ValidSymbols))]
    public Task Verify(string[] symbols)
    {
        var schema = TestSchemas.Get("enum").With("symbols", symbols).ToString();

        return TestHelper.VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidSymbols))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("enum").With("symbols", JsonNode.Parse(json)!).ToString();

        return TestHelper.VerifyDiagnostic(schema);
    }

    public static TheoryData<string[]> ValidSymbols() => new(
        [[], ["A", "B"]]);

    public static TheoryData<string> InvalidSymbols() => new(
        ["null", "{}"]);
}
