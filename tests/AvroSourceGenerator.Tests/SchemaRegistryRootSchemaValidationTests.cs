using System.Text.Json;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaRegistryRootSchemaValidationTests
{
    [Theory]
    [MemberData(nameof(InvalidRootSchemas))]
    public void Register_InvalidRootSchema_ThrowsInvalidSchemaException(string json)
    {
        var schema = Parse(json);

        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);

        var exception = Assert.Throws<InvalidSchemaException>(() => registry.Register(schema));

        Assert.Contains("At least a named schema must be present", exception.Message);
    }

    [Theory]
    [MemberData(nameof(InvalidRootSchemas))]
    public void Register_InvalidRootSchemaAfterNamedSchema_StillThrowsInvalidSchemaException(string json)
    {
        var record = Parse(
            """
            {
              "type": "record",
              "name": "OrderCreated",
              "namespace": "Demo",
              "fields": []
            }
            """);
        var schema = Parse(json);

        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);
        registry.Register(record);

        var exception = Assert.Throws<InvalidSchemaException>(() => registry.Register(schema));

        Assert.Contains("At least a named schema must be present", exception.Message);
    }

    [Theory]
    [MemberData(nameof(ValidRootSchemas))]
    public void Register_ValidRootSchema_RegistersNamedSchema(string json)
    {
        var schema = Parse(json);

        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);
        registry.Register(schema);

        Assert.Contains(registry.Schemas.Values, x => x is RecordSchema);
    }

    public static TheoryData<string> InvalidRootSchemas() =>
    [
        "\"string\"",
        """
        {
          "type": "array",
          "items": "string"
        }
        """,
        """
        {
          "type": "map",
          "values": "string"
        }
        """
    ];

    public static TheoryData<string> ValidRootSchemas() =>
    [
        """
        {
          "type": "array",
          "items": {
            "type": "record",
            "namespace": "Demo",
            "name": "ArrayRecord",
            "fields": []
          }
        }
        """,
        """
        {
          "type": "map",
          "values": {
            "type": "record",
            "namespace": "Demo",
            "name": "MapRecord",
            "fields": []
          }
        }
        """,
        """
        [
          "null",
          {
            "type": "record",
            "namespace": "Demo",
            "name": "UnionRecord",
            "fields": []
          }
        ]
        """
    ];

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
