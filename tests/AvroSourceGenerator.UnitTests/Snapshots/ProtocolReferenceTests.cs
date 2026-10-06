namespace AvroSourceGenerator.UnitTests.Snapshots;

public sealed class ProtocolReferenceTests
{
    [Fact]
    public Task Diagnostic() => Snapshot.Diagnostic(
        ProjectFile.Protocol(
            """
            {
                "protocol": "RpcProtocol",
                "namespace": "SchemaNamespace",
                "types": [],
                "messages": {
                    "GetMissing": {
                        "request": [],
                        "response": "MissingResponse"
                    }
                }
            }
            """));
}
