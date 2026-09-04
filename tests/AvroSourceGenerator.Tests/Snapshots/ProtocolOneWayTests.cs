using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class ProtocolOneWayTests
{
    [Fact]
    public void Lower_ExplicitFalse_PreservesOneWayWhenWritingProtocol()
    {
        var (schemas, protocol) = LowerProtocol("""
            ,
                        "one-way": false
            """);
        var message = Assert.Single(protocol.Messages);

        var json = protocol.ToJsonString(schemas);

        Assert.False(message.OneWay);
        Assert.Contains("\"one-way\":false", json);
    }

    [Fact]
    public void Lower_Protocol_RequiresNullability()
    {
        var (_, protocol) = LowerProtocol();

        Assert.True(protocol.RequiresNullability);
    }

    [Fact]
    public Task Diagnostic_InvalidResponse() => Snapshot.Diagnostic(
        ProjectFile.Protocol(
            """
            {
                "protocol": "InvalidService",
                "types": [],
                "messages": {
                    "heartbeat": {
                        "request": [],
                        "response": "string",
                        "one-way": true
                    }
                }
            }
            """));

    private static (IReadOnlyDictionary<SchemaName, TopLevelSchema> Schemas, ProtocolSchema Protocol) LowerProtocol(string oneWayJson = "")
    {
        var parsed = SchemaCompilerTestHelpers.ParseJson(
            $$"""
            {
                "protocol": "Service",
                "types": [],
                "messages": {
                    "ping": {
                        "request": [],
                        "response": "null"{{oneWayJson}}
                    }
                }
            }
            """);
        var schemas = parsed.Declarations.ToDictionary(static schema => schema.SchemaName);
        return (schemas, Assert.IsType<ProtocolSchema>(Assert.Single(parsed.Declarations)));
    }
}
