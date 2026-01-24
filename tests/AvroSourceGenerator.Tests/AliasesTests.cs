namespace AvroSourceGenerator.Tests;

public sealed class AliasesTests
{
    [Theory]
    [MemberData(nameof(InvalidAliasesSchemaPairs))]
    public Task Diagnostic(string json, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("aliases", JsonNode.Parse(json)!).ToString();

        return VerifyDiagnostic(schema);
    }

    // TODO: What to do with aliases?
    public static MatrixTheoryData<string[], string> ValidAliasesSchemaPairs() => new(
        [null!, [], ["Alias1", "Alias2"]],
        ["enum", "error", "fixed", "record"]);

    public static MatrixTheoryData<string, string> InvalidAliasesSchemaPairs() => new(
        ["{}"],
        ["enum", "error", "fixed", "record"]);
}
