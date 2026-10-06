namespace AvroSourceGenerator.UnitTests.Apache.Snapshots;

public class LogicalTests
{
    [Fact]
    public Task Verify_Decimal_Bytes() => Snapshot.Schema(
        """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Container",
            "fields": [
                {
                    "name": "DecimalField",
                    "type": {
                        "type": "bytes",
                        "logicalType": "decimal",
                        "precision": 4,
                        "scale": 2
                    }
                }
            ]
        }
        """);

    [Fact]
    public Task Verify_Decimal_Fixed()
    {
        var fixedSchema = TestSchemas.Get("fixed").With("name", "Decimal").With("size", 20)
            .With("logicalType", "decimal").With("precision", 4).With("scale", 2);
        var fields = new JsonArray
        {
            new JsonObject { ["name"] = "First", ["type"] = fixedSchema },
            new JsonObject { ["name"] = "Second", ["type"] = "Decimal" }
        };
        var schema = TestSchemas.Get("record").With("name", "Container").With("fields", fields);

        return Snapshot.Schema(schema.ToString());
    }

    [Fact]
    public Task Verify_Uuid_String() => Snapshot.Schema(
        """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Container",
            "fields": [
                {
                    "name": "UuidField",
                    "type": {
                        "type": "string",
                        "logicalType": "uuid"
                    }
                }
            ]
        }
        """);

    [Fact]
    public Task Verify_Uuid_Fixed() => Snapshot.Schema(
        """
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Uuid",
            "size": 16,
            "logicalType": "uuid"
        }
        """);

    [Fact]
    public Task Verify_Date() => Snapshot.Schema(
        """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Container",
            "fields": [
                {
                    "name": "DateField",
                    "type": {
                        "type": "int",
                        "logicalType": "date"
                    }
                }
            ]
        }
        """);

    [Fact]
    public Task Verify_Time_Milliseconds() => Snapshot.Schema(
        """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Container",
            "fields": [
                {
                    "name": "TimeField",
                    "type": {
                        "type": "int",
                        "logicalType": "time-millis"
                    }
                }
            ]
        }
        """);

    [Fact]
    public Task Verify_Time_Microseconds() => Snapshot.Schema(
        """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Container",
            "fields": [
                {
                    "name": "TimeField",
                    "type": {
                        "type": "int",
                        "logicalType": "time-micros"
                    }
                }
            ]
        }
        """);

    [Fact]
    public Task Verify_Timestamp_Milliseconds() => Snapshot.Schema(
        """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Container",
            "fields": [
                {
                    "name": "TimestampField",
                    "type": {
                        "type": "long",
                        "logicalType": "timestamp-millis"
                    }
                }
            ]
        }
        """);

    [Fact]
    public Task Verify_Timestamp_Microseconds() => Snapshot.Schema(
        """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Container",
            "fields": [
                {
                    "name": "TimestampField",
                    "type": {
                        "type": "long",
                        "logicalType": "timestamp-micros"
                    }
                }
            ]
        }
        """);

    [Fact]
    public Task Verify_Local_Timestamp_Milliseconds() => Snapshot.Schema(
        """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Container",
            "fields": [
                {
                    "name": "TimestampField",
                    "type": {
                        "type": "long",
                        "logicalType": "local-timestamp-millis"
                    }
                }
            ]
        }
        """);

    [Fact]
    public Task Verify_Local_Timestamp_Microseconds() => Snapshot.Schema(
        """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Container",
            "fields": [
                {
                    "name": "TimestampField",
                    "type": {
                        "type": "long",
                        "logicalType": "local-timestamp-micros"
                    }
                }
            ]
        }
        """);

    [Fact]
    public Task Verify_Duration() => Snapshot.Schema(
        """
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Duration",
            "size": 12,
            "logicalType": "duration"
        }
        """);
}
