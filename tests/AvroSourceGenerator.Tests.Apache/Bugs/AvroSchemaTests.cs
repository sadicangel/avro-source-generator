namespace AvroSourceGenerator.Tests.Apache.Bugs;

public class AvroSchemaTests
{
    // TODO: Language implementations must ignore unknown logical types when reading, and should use the underlying Avro type.
    // Apache.Avro will throw an exception when it encounters an unknown logical type, which is not compliant.
    // We avoid this behaviour in AvroSourceGenerator by erasing unsupported logical types from the schema before
    // passing it to Avro.Schema.Parse. Although this workaround works for code generation, it will probably cause
    // issues with validating schemas against schema registries that return schemas with unsupported logical types.
    // 
    // These PRs seem to be trying to address this issue:
    // https://github.com/apache/avro/pull/2512
    // https://github.com/apache/avro/pull/2751

    [Fact]
    public void Parse_throws_for_duration_logical_type()
    {
        Assert.Throws<Avro.SchemaParseException>(() => Avro.Schema.Parse(
            """
            {
                "type": "fixed",
                "name": "Duration",
                "size": 12,
                "logicalType": "duration"
            }
            """));
    }

    [Fact]
    public void Parse_throws_for_decimal_logical_type_with_fixed_as_underlying_type()
    {
        Assert.Throws<Avro.SchemaParseException>(() => Avro.Schema.Parse(
            """
            {
                "type": "fixed",
                "name": "Decimal",
                "size": 20,
                "logicalType": "decimal",
                "precision": 4,
                "scale": 2
            }
            """));
    }

    [Fact]
    public void Parse_throws_for_uuid_logical_type_with_fixed_as_underlying_type()
    {
        Assert.Throws<Avro.SchemaParseException>(() => Avro.Schema.Parse(
            """
            {
                "type": "fixed",
                "name": "Uuid",
                "size": 16,
                "logicalType": "uuid"
            }
            """));
    }

    [Fact]
    public void Parse_throws_for_unknown_logical_type()
    {
        Assert.Throws<Avro.SchemaParseException>(() => Avro.Schema.Parse(
            $$"""
            {
                "type": "fixed",
                "name": "Duration",
                "size": 12,
                "logicalType": "type{{Guid.NewGuid():N}}"
            }
            """));
    }
}
