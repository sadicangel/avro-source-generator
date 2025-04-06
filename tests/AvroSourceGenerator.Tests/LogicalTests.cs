using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;
public class LogicalTests
{
    [Theory]
    [InlineData("bytes", "4", "2"), InlineData("bytes", "4", "null"), InlineData("fixed", "4", "2"), InlineData("fixed", "4", "null")]
    public Task Verify_Decimal(string type, string precision, string scale) => TestHelper.VerifySourceCode($$"""
    {
        "type": "record",
        "namespace": "SchemaNamespace",
        "name": "Container",
        "fields": [
            {
                "name": "DecimalField",
                "type": {
                    "type": "{{type}}",
                    "logicalType": "decimal",
                    "precision": {{precision}},
                    "scale": {{scale}}
                }
            }
        ]
    }
    """);

    [Fact]
    public Task Verify_Uuid() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Date() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Time_Milliseconds() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Time_Microseconds() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Timestamp_Milliseconds() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Timestamp_Microseconds() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Local_Timestamp_Milliseconds() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Local_Timestamp_Microseconds() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Duration() => TestHelper.VerifySourceCode("""
    {
        "type": "fixed",
        "name": "Duration",
        "size": 12,
        "logicalType": "duration"
    }
    """);
}
