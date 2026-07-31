namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class ProtocolExtensionTests
{
    [Fact]
    public Task Verify_AvscProtocolCompatibility() =>
        Snapshot.Schema(TestSchemas.Get("protocol").ToString());
}
