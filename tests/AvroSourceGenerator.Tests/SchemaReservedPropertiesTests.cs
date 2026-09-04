using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaReservedPropertiesTests
{
    [Fact]
    public void Lower_RecordAndField_ExcludeReservedPropertiesFromCustomProperties()
    {
        var parsed = SchemaCompilerTestHelpers.ParseJson(
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

        var record = Assert.IsType<RecordSchema>(Assert.Single(parsed.Declarations));
        var field = Assert.Single(record.Fields);

        Assert.Equal(["x-record"], record.Properties.Keys);
        Assert.Equal(["x-field"], field.Properties.Keys);
    }

    [Fact]
    public void Lower_Protocol_ExcludeReservedProtocolPropertiesFromCustomProperties()
    {
        var parsed = SchemaCompilerTestHelpers.ParseJson(
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

        var protocol = Assert.IsType<ProtocolSchema>(Assert.Single(parsed.Declarations));

        Assert.Equal(["x-protocol"], protocol.Properties.Keys);
    }
}
