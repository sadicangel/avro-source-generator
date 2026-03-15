using System.Text.Json;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaRegistryReservedPropertiesTests
{
    [Fact]
    public void Register_RecordAndField_ExcludeReservedPropertiesFromCustomProperties()
    {
        var schema = Parse(
            """
            {
              "type": "record",
              "name": "OrderCreated",
              "namespace": "Demo",
              "doc": "record doc",
              "aliases": ["OrderCreatedV1"],
              "x-record": "custom",
              "fields": [
                {
                  "name": "Id",
                  "type": "string",
                  "doc": "field doc",
                  "aliases": ["LegacyId"],
                  "order": 1,
                  "default": "A",
                  "x-field": true
                }
              ]
            }
            """);

        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);
        registry.RegisterSchema(schema);
        var record = Assert.IsType<RecordSchema>(Assert.Single(registry.Schemas.Values));
        var field = Assert.Single(record.Fields);

        Assert.Equal(["x-record"], record.Properties.Keys);
        Assert.Equal(["x-field"], field.Properties.Keys);
    }

    [Fact]
    public void Register_Protocol_ExcludeReservedProtocolPropertiesFromCustomProperties()
    {
        var schema = Parse(
            """
            {
              "protocol": "UserApi",
              "namespace": "Demo",
              "types": [],
              "messages": {
                "Ping": {
                  "request": [],
                  "response": "null"
                }
              },
              "x-protocol": 1
            }
            """);

        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);
        registry.RegisterSchema(schema);
        var protocol = Assert.IsType<ProtocolSchema>(Assert.Single(registry.Schemas.Values));

        Assert.Equal(["x-protocol"], protocol.Properties.Keys);
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
