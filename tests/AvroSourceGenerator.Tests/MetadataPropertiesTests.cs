using System.Text.Json.Nodes;
using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class MetadataPropertiesTests
{
    [Theory]
    [InlineData("error")]
    [InlineData("fixed")]
    [InlineData("record")]
    public Task Verify(string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("metadata", JsonArray.Parse("[\"Tag1\", \"Tag2\"]")!).ToString();

        return TestHelper.VerifySourceCode(schema);
    }
}
