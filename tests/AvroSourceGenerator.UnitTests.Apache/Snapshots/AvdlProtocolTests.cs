namespace AvroSourceGenerator.UnitTests.Apache.Snapshots;

public sealed class AvdlProtocolTests
{
    [Fact]
    public Task Verify() => Snapshot.Source(TestSources.Get("avdl.protocol"));
}
