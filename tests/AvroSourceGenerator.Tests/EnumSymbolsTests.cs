namespace AvroSourceGenerator.Tests;

public class EnumSymbolsTests
{
    [Theory]
    [MemberData(nameof(ValidSymbols))]
    public Task Verify(string[] symbols)
    {
        var schema = TestSchemas.Get("enum").With("symbols", symbols).ToString();

        return VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidSymbols))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("enum").With("symbols", JsonNode.Parse(json)!).ToString();

        return VerifyDiagnostic(schema);
    }

    public static TheoryData<string[]> ValidSymbols() => new(
        [[], ["A", "B"]]);

    public static TheoryData<string> InvalidSymbols() => new(
        ["null", "{}"]);
}
