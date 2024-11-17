namespace AvroSourceGenerator.Example;

#if NET8_0_OR_GREATER
[AvroModel(AvroModelFeatures.Net8)]
#else
[AvroModel(AvroModelFeatures.Net472)]
#endif
public partial record class Test
{
    public const string SchemaJson = """
        {
            "type": "record",
            "name": "Test",
            "namespace": "com.example.avro",
            "fields": [
                { "name": "null_field", "type": "null"},
                { "name": "boolean_field", "type": "boolean"},
                { "name": "int_field", "type": "int"},
                { "name": "long_field", "type": "long"},
                { "name": "float_field", "type": "float"},
                { "name": "double_field", "type": "double"},
                { "name": "string_field", "type": "string"},
                { "name": "bytes_field", "type": "bytes"},
                { "name": "array_field", "type": { "type": "array", "items": "int"} },
                { "name": "array_field_null", "type": { "type": "array", "items": ["null", "int"]} },
                { "name": "map_field", "type": { "type": "map", "values": "string"} },
                { "name": "map_field_null", "type": { "type": "map", "values": ["null", "string"]} },
                { "name": "enum_field", "type": { "type": "enum", "name": "ExampleEnum", "symbols": ["SYMBOL1", "SYMBOL2"]} },
                { "name": "fixed_field", "type": { "type": "fixed", "name": "ExampleFixed", "size": 16} },
                { "name": "union_field", "type": ["string", "int", { "type": "record", "name": "UnionRecord", "fields": [{ "name": "sub_field", "type": "string"}]}]},
                { "name": "record_field", "type": { "type": "record", "name": "ExampleRecord", "fields": [{ "name": "sub_field", "type": "string"}]} },
                { "name": "decimal_field_bytes", "type": { "type": "bytes", "logicalType": "decimal", "precision": 2 } },
                { "name": "uuid", "type": { "type": "string", "logicalType": "uuid" } },
                { "name": "date", "type": { "type": "int", "logicalType": "date" } },
                { "name": "time_ms", "type": { "type": "int", "logicalType": "time-millis" } },
                { "name": "time_us", "type": { "type": "long", "logicalType": "time-micros" } },
                { "name": "timestamp_ms", "type": { "type": "long", "logicalType": "timestamp-millis" } },
                { "name": "timestamp_us", "type": { "type": "long", "logicalType": "timestamp-micros" } },
                { "name": "local_timestamp_ms", "type": { "type": "long", "logicalType": "local-timestamp-millis" } },
                { "name": "local_timestamp_us", "type": { "type": "long", "logicalType": "local-timestamp-micros" } },
                { "name": "null_boolean_field", "type": ["null", "boolean"]},
                { "name": "null_int_field", "type": ["null", "int"]},
                { "name": "null_long_field", "type": ["null", "long"]},
                { "name": "null_float_field", "type": ["null", "float"]},
                { "name": "null_double_field", "type": ["null", "double"]},
                { "name": "null_string_field", "type": ["null", "string"]},
                { "name": "null_bytes_field", "type": ["null", "bytes"]},
                { "name": "null_array_field", "type": ["null", { "type": "array", "items": "int"} ]},
                { "name": "null_array_field_null", "type": ["null", { "type": "array", "items": ["null", "int"]} ]},
                { "name": "null_map_field", "type": ["null", { "type": "map", "values": "string"} ]},
                { "name": "null_map_field_null", "type": ["null", { "type": "map", "values": ["null", "string"]} ]},
                { "name": "null_enum_field", "type": ["null", "ExampleEnum" ]},
                { "name": "null_fixed_field", "type": ["null", "ExampleFixed" ]},
                { "name": "null_union_field", "type": ["null", "string", "int", "UnionRecord" ]},
                { "name": "null_record_field", "type": ["null", "ExampleRecord" ]},
                { "name": "null_decimal_field_bytes", "type": ["null", { "type": "bytes", "logicalType": "decimal", "precision": 2 }]},
                { "name": "null_uuid", "type": ["null", { "type": "string", "logicalType": "uuid" }]},
                { "name": "null_date", "type": ["null", { "type": "int", "logicalType": "date" }]},
                { "name": "null_time_ms", "type": ["null", { "type": "int", "logicalType": "time-millis" }]},
                { "name": "null_time_us", "type": ["null", { "type": "long", "logicalType": "time-micros" }]},
                { "name": "null_timestamp_ms", "type": ["null", { "type": "long", "logicalType": "timestamp-millis" }]},
                { "name": "null_timestamp_us", "type": ["null", { "type": "long", "logicalType": "timestamp-micros" }]},
                { "name": "null_local_timestamp_ms", "type": ["null", { "type": "long", "logicalType": "local-timestamp-millis" }]},
                { "name": "null_local_timestamp_us", "type": ["null", { "type": "long", "logicalType": "local-timestamp-micros" }]}
            ]
        }
    """;
}

