namespace AvroSourceGenerator.Tests;

public sealed class UnsupportedLogicalTests
{
    [Fact]
    public Task Verify()
    {
        var schema = TestSchemas.Get("record")
            .With(
                "fields",
                [
                    new JsonObject
                    {
                        ["name"] = "fixedField",
                        ["type"] = TestSchemas.Get("fixed").With("logicalType", "someType")
                    }
                ]).ToString();

        return VerifySourceCode(schema);
    }
}
