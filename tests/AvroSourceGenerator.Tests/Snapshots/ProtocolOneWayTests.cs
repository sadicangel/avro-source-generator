using System.Text.Json;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Registry;

namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class ProtocolOneWayTests
{
    [Fact]
    public void Register_ExplicitFalse_PreservesOneWayWhenWritingProtocol()
    {
        var (registry, protocol) = RegisterProtocol("""
            ,
                        "one-way": false
            """);
        var message = Assert.Single(protocol.Messages);

        var json = protocol.ToJsonString(registry.Schemas);

        Assert.False(message.OneWay);
        Assert.Contains("\"one-way\":false", json);
    }

    [Fact]
    public void Register_Protocol_RequiresNullability()
    {
        var (_, protocol) = RegisterProtocol();

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

    private static (SchemaRegistry Registry, ProtocolSchema Protocol) RegisterProtocol(string oneWayJson = "")
    {
        var schema = Parse(
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
        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);
        registry.RegisterSchema(schema);
        return (registry, Assert.IsType<ProtocolSchema>(Assert.Single(registry.Schemas.Values)));
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
