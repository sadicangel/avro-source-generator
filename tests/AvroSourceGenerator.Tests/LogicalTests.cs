﻿using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;
public class LogicalTests
{
    [Fact]
    public Task Verify_Decimal_Bytes() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Decimal_Fixed() => TestHelper.VerifySourceCode("""
    {
        "type": "record",
        "namespace": "SchemaNamespace",
        "name": "Container",
        "fields": [
            {
                "name": "DecimalField",
                "type": {
                    "type": "fixed",
                    "name": "Decimal",
                    "size": 20,
                    "logicalType": "decimal",
                    "precision": 4,
                    "scale": 2
                }
            }
        ]
    }
    """);

    [Fact]
    public Task Verify_Uuid_String() => TestHelper.VerifySourceCode("""
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
    public Task Verify_Uuid_Fixed() => TestHelper.VerifySourceCode("""
    {
        "type": "record",
        "namespace": "SchemaNamespace",
        "name": "Container",
        "fields": [
            {
                "name": "UuidField",
                "type": {
                    "type": "fixed",
                    "name": "Uuid",
                    "size": 16,
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
