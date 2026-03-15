namespace AvroSourceGenerator.Tests.Snapshots;

public class EnumSymbolsTests
{
    [Theory]
    [MemberData(nameof(ValidSymbols))]
    public Task Verify(string[] symbols)
    {
        var schema = TestSchemas.Get("enum").With("symbols", symbols).ToString();

        return Snapshot.Schema(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidSymbols))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("enum").With("symbols", JsonNode.Parse(json)!).ToString();

        return Snapshot.Diagnostic(schema);
    }

    public static TheoryData<string[]> ValidSymbols() => new TheoryData<string[]>([], ["A", "B"]);

    public static TheoryData<string> InvalidSymbols() => new TheoryData<string>("null", "{}");
}
