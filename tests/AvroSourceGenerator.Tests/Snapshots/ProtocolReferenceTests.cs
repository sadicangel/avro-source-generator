namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class ProtocolReferenceTests
{
    [Fact]
    public Task Diagnostic() => Snapshot.Diagnostic(
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
        """);
}
