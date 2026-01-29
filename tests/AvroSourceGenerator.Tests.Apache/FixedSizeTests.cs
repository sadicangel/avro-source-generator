namespace AvroSourceGenerator.Tests.Apache;

public sealed class FixedSizeTests
{
    [Theory]
    [MemberData(nameof(ValidSizes))]
    public Task Verify(int size)
    {
        var schema = TestSchemas.Get("fixed").With("size", size).ToString();

        return VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(InvalidSizes))]
    public Task Diagnostic(string json)
    {
        var schema = TestSchemas.Get("fixed").With("size", JsonNode.Parse(json)!).ToString();

        return VerifyDiagnostic(schema);
    }

    public static TheoryData<int> ValidSizes() => new TheoryData<int>(32);

    public static TheoryData<string> InvalidSizes() => new TheoryData<string>("null", "-1", "0", "1.1", "{}");
}
