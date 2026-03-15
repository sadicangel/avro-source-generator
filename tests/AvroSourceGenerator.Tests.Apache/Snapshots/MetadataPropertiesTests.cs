namespace AvroSourceGenerator.Tests.Apache.Snapshots;

public sealed class MetadataPropertiesTests
{
    [Theory]
    [InlineData("error")]
    [InlineData("fixed")]
    [InlineData("record")]
    public Task Verify(string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).With("metadata", JsonNode.Parse("[\"Tag1\", \"Tag2\"]")!).ToString();

        return Snapshot.Schema(schema);
    }
}
