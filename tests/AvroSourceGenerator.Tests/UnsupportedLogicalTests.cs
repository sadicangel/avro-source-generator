namespace AvroSourceGenerator.Tests;

public sealed class UnsupportedLogicalTests
{
    [Fact]
    public Task Verify()
    {
        var schema = TestSchemas.Get("fixed").With("logicalType", "someType").ToString();

        return VerifySourceCode(schema);
    }
}
