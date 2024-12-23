﻿namespace AvroSourceGenerator.IntegrationTests;

[Avro(AvroSchema, LanguageFeatures = LanguageFeatures.CSharp13, UseCSharpNamespace = true)]
public partial class Record
{
    private const string AvroSchema = """
    {
        "type": "record",
        "namespace": "SchemaNamespace",
        "name": "Record",
        "fields": [
            { "name": "BooleanField", "type": "boolean" },
            { "name": "IntField", "type": "int" },
            { "name": "LongField", "type": "long" },
            { "name": "FloatField", "type": "float" },
            { "name": "DoubleField", "type": "double" },
            { "name": "BytesField", "type": "bytes" },
            { "name": "StringField", "type": "string" },
            { "name": "EnumField", "type": { 
                "type": "enum",
                "name": "TestEnum",
                "symbols": [
                    "A", "B", "C"
                ]
            } },
            { "name": "ArrayField", "type": { "type": "array", "items": "string" } },
            { "name": "MapField", "type": { "type": "map", "values": "long" } },
            { "name": "FixedField", "type": { "type": "fixed", "name": "FixedType", "size": 16 } },
            { "name": "UnionField", "type": ["int", "string"] },
            { "name": "RecordField", "type": {
                "type": "record",
                "name": "NestedRecord",
                "fields": [
                    { "name": "NestedField", "type": "int" }
                ]
            } },
            { "name": "DecimalField", "type": {
                "type": "bytes",
                "logicalType": "decimal",
                "precision": 9,
                "scale": 2
            } },
            { "name": "UuidField", "type": {
                "type": "string",
                "logicalType": "uuid"
            } },
            { "name": "DateField", "type": {
                "type": "int",
                "logicalType": "date"
            } },
            { "name": "TimeMillisField", "type": {
                "type": "int",
                "logicalType": "time-millis"
            } },
            { "name": "TimeMicrosField", "type": {
                "type": "long",
                "logicalType": "time-micros"
            } },
            { "name": "TimestampMillisField", "type": {
                "type": "long",
                "logicalType": "timestamp-millis"
            } },
            { "name": "TimestampMicrosField", "type": {
                "type": "long",
                "logicalType": "timestamp-micros"
            } },
            { "name": "LocalTimestampMillisField", "type": {
                "type": "long",
                "logicalType": "local-timestamp-millis"
            } },
            { "name": "LocalTimestampMicrosField", "type": {
                "type": "long",
                "logicalType": "local-timestamp-micros"
            } },
            { "name": "DurationField", "type": {
                "type": "fixed",
                "name": "Duration",
                "size": 12,
                "logicalType": "duration"
            } }
        ]
    }
    """;
}
