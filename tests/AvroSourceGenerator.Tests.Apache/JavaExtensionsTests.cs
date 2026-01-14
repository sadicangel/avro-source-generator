namespace AvroSourceGenerator.Tests.Apache;

public sealed class JavaExtensionsTests
{
    [Fact]
    public Task Verify() => VerifySourceCode(
        """
        {
          "name": "StringBehaviorTest",
          "namespace": "org.apache.parquet.avro",
          "type": "record",
          "fields": [
            {
              "name": "default_class",
              "type": "string"
            },
            {
              "name": "string_class",
              "type": {
                "type": "string",
                "avro.java.string": "String"
              }
            },
            {
              "name": "stringable_class",
              "type": {
                "type": "string",
                "java-class": "java.math.BigDecimal"
              }
            },
            {
              "name": "default_map",
              "type": {
                "type": "map",
                "values": "int"
              }
            },
            {
              "name": "string_map",
              "type": {
                "type": "map",
                "values": "int",
                "avro.java.string": "String"
              }
            },
            {
              "name": "stringable_map",
              "type": {
                "type": "map",
                "values": "int",
                "java-key-class": "java.math.BigDecimal"
              }
            }
          ]
        }
        """);
}
