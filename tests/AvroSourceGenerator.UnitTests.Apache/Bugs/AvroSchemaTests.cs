using Avro;
using Avro.Generic;
using Avro.IO;
using Avro.Util;

namespace AvroSourceGenerator.UnitTests.Apache.Bugs;

public class AvroSchemaTests
{
    [Fact]
    public void Parse_uses_underlying_type_for_duration()
    {
        var source = TestSchemas.Get("fixed").With("name", "Duration").With("size", 12).With("logicalType", "duration");
        var schema = Assert.IsType<LogicalSchema>(Schema.Parse(source.ToString()));

        Assert.IsType<FixedSchema>(schema.BaseSchema);
        Assert.IsType<UnknownLogicalType>(schema.LogicalType);
        Assert.IsType<JsonObject>(JsonNode.Parse(schema.ToString())!["type"]);
    }

    [Fact]
    public void Parse_accepts_duration_fixed_inside_record()
    {
        var fixedSchema = TestSchemas.Get("fixed").With("name", "SubscriptionDuration").With("size", 12)
            .With("logicalType", "duration");
        fixedSchema.AsObject().Remove("namespace");
        var fields = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "date",
                ["type"] = new JsonObject
                {
                    ["type"] = "int",
                    ["logicalType"] = "date"
                }
            },
            new JsonObject
            {
                ["name"] = "price",
                ["type"] = new JsonObject
                {
                    ["type"] = "bytes",
                    ["logicalType"] = "decimal",
                    ["precision"] = 10,
                    ["scale"] = 2
                }
            },
            new JsonObject
            {
                ["name"] = "duration",
                ["type"] = fixedSchema,
                ["doc"] = "Duration as months/days/milliseconds (12 bytes total)."
            },
            new JsonObject
            {
                ["name"] = "future",
                ["type"] = new JsonObject
                {
                    ["type"] = "string",
                    ["logicalType"] = "future-type"
                }
            }
        };
        var source = TestSchemas.Get("record").With("name", "LogicalTypes")
            .With("namespace", "AvroSourceGenerator.IntegrationTests.Schemas").With("fields", fields);

        var schema = Assert.IsType<RecordSchema>(Schema.Parse(source.ToString()));
        Assert.IsType<LogicalSchema>(schema.Fields[2].Schema);
    }

    [Fact]
    public void Parse_maps_fixed_decimal_to_avro_decimal()
    {
        var source = TestSchemas.Get("fixed").With("name", "Decimal").With("size", 20)
            .With("logicalType", "decimal").With("precision", 4).With("scale", 2);
        var schema = Assert.IsType<LogicalSchema>(Schema.Parse(source.ToString()));

        Assert.IsType<FixedSchema>(schema.BaseSchema);
        Assert.Equal(typeof(AvroDecimal), schema.LogicalType.GetCSharpType(false));
    }

    [Fact]
    public void Generic_fixed_decimal_round_trips()
    {
        var source = TestSchemas.Get("fixed").With("name", "Decimal").With("size", 8)
            .With("logicalType", "decimal").With("precision", 12).With("scale", 2);
        var schema = Schema.Parse(source.ToString());
        var expected = new AvroDecimal(1234.56m);

        using var stream = new MemoryStream();
        new GenericDatumWriter<AvroDecimal>(schema).Write(expected, new BinaryEncoder(stream));
        stream.Position = 0;

        var actual = new GenericDatumReader<AvroDecimal>(schema, schema)
            .Read(default, new BinaryDecoder(stream));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Parse_still_throws_for_uuid_with_fixed_underlying_type()
    {
        var source = TestSchemas.Get("fixed").With("name", "Uuid").With("logicalType", "uuid");
        Assert.Throws<AvroTypeException>(() => Schema.Parse(source.ToString()));
    }

    [Theory]
    [InlineData("bytes", "uuid")]
    [InlineData("int", "decimal")]
    public void Parse_still_throws_for_unsupported_primitive_logical_types(string type, string logicalType)
    {
        var source = new JsonObject
        {
            ["type"] = TestSchemas.Get(type),
            ["logicalType"] = logicalType
        };
        if (logicalType is "decimal") source["precision"] = 4;

        Assert.Throws<AvroTypeException>(() => Schema.Parse(source.ToString()));
    }

    [Fact]
    public void Parse_named_fixed_reference_uses_underlying_fixed_type()
    {
        var fixedSchema = TestSchemas.Get("fixed").With("name", "Amount").With("size", 8)
            .With("logicalType", "decimal").With("precision", 12).With("scale", 2);
        var fields = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "first",
                ["type"] = fixedSchema
            },
            new JsonObject
            {
                ["name"] = "second",
                ["type"] = "Amount"
            }
        };
        var source = TestSchemas.Get("record").With("fields", fields);
        var schema = Assert.IsType<RecordSchema>(Schema.Parse(source.ToString()));

        Assert.IsType<LogicalSchema>(schema.Fields[0].Schema);
        Assert.IsType<FixedSchema>(schema.Fields[1].Schema);
    }

    [Theory]
    [InlineData("string", "hello")]
    [InlineData("int", 42)]
    public void Parse_uses_underlying_value_for_unknown_logical_type(string type, object value)
    {
        var source = new JsonObject
        {
            ["type"] = TestSchemas.Get(type),
            ["logicalType"] = "future-type"
        };
        var schema = Assert.IsType<LogicalSchema>(Schema.Parse(source.ToString()));

        Assert.IsType<UnknownLogicalType>(schema.LogicalType);
        Assert.Equal(value, schema.LogicalType.ConvertToLogicalValue(value, schema));
        Assert.Equal(value, schema.LogicalType.ConvertToBaseValue(value, schema));
    }

    [Fact]
    public void Parse_uses_underlying_fixed_for_unknown_logical_type()
    {
        var source = TestSchemas.Get("fixed").With("name", "Future").With("size", 2).With("logicalType", "future-type");
        var schema = Assert.IsType<LogicalSchema>(Schema.Parse(source.ToString()));
        var value = new GenericFixed((FixedSchema)schema.BaseSchema, [1, 2]);

        Assert.IsType<UnknownLogicalType>(schema.LogicalType);
        Assert.Same(value, schema.LogicalType.ConvertToLogicalValue(value, schema));
    }

    [Fact]
    public void Parse_accepts_unknown_logical_type_inside_record()
    {
        var fields = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "future",
                ["type"] = new JsonObject
                {
                    ["type"] = "string",
                    ["logicalType"] = "future-type"
                }
            }
        };
        var source = TestSchemas.Get("record").With("fields", fields);

        var schema = Assert.IsType<RecordSchema>(Schema.Parse(source.ToString()));
        Assert.IsType<LogicalSchema>(schema.Fields[0].Schema);
    }
}
