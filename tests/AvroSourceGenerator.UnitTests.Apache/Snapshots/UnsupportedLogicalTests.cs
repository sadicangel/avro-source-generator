namespace AvroSourceGenerator.UnitTests.Apache.Snapshots;

public sealed class UnsupportedLogicalTests
{
    [Fact]
    public Task Verify()
    {
        var schema = TestSchemas.Get("fixed").With("logicalType", "someType").ToString();

        return Snapshot.Schema(schema);
    }

    [Fact]
    public Task Verify_unknown_primitive_field()
    {
        var fieldType = new JsonObject { ["type"] = "string", ["logicalType"] = "future-type" };
        var fields = new JsonArray { new JsonObject { ["name"] = "Value", ["type"] = fieldType } };
        var schema = TestSchemas.Get("record").With("name", "Container").With("fields", fields);

        return Snapshot.Schema(schema.ToString());
    }

    [Fact]
    public Task Verify_ignored_known_primitive_fields()
    {
        var fields = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "UnsupportedUuid",
                ["type"] = new JsonObject { ["type"] = "bytes", ["logicalType"] = "uuid" }
            },
            new JsonObject
            {
                ["name"] = "UnsupportedDecimal",
                ["type"] = new JsonObject { ["type"] = "int", ["logicalType"] = "decimal", ["precision"] = 4 }
            }
        };
        var schema = TestSchemas.Get("record").With("name", "Container").With("fields", fields);

        return Snapshot.Schema(schema.ToString());
    }
}
