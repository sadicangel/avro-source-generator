namespace AvroSourceGenerator.UnitTests.Snapshots;

public sealed class AvdlSchemaTests
{
    [Fact]
    public Task Verify() => Snapshot.Source(TestSources.Get("avdl.schema"));
}
