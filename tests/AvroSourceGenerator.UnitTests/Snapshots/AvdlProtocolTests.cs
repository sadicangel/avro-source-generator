namespace AvroSourceGenerator.UnitTests.Snapshots;

public sealed class AvdlProtocolTests
{
    [Fact]
    public Task Verify() => Snapshot.Source(TestSources.Get("avdl.protocol"));
}
