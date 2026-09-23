namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class ProtocolExtensionTests
{
    [Fact]
    public Task Verify_AvprProtocol() =>
        Snapshot.Protocol(TestSchemas.Get("protocol").ToString());
}
