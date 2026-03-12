using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaRegistryDuplicateResolutionTests
{
    [Fact]
    public void Register_IgnoreDuplicateInlineSchema_KeepsExistingSchemaAvailableInCurrentScope()
    {
        var registry = new SchemaRegistry(
            SchemaRegistryOptions.Default with
            {
                DuplicateResolution = DuplicateResolution.Ignore,
            });

        registry.Register(
            Parse(
                """
                {
                  "type": "record",
                  "name": "SharedRecord",
                  "namespace": "Demo",
                  "fields": []
                }
                """));

        registry.Register(
            Parse(
                """
                {
                  "type": "record",
                  "name": "Container",
                  "namespace": "Demo",
                  "fields": [
                    {
                      "name": "InlineShared",
                      "type": {
                        "type": "record",
                        "name": "SharedRecord",
                        "namespace": "Demo",
                        "fields": []
                      }
                    },
                    {
                      "name": "ReferencedShared",
                      "type": "SharedRecord"
                    }
                  ]
                }
                """));

        var container = Assert.IsType<RecordSchema>(registry.Schemas[new SchemaName("Container", "Demo")]);
        var referencedField = Assert.Single(container.Fields.Where(static field => field.Name == "ReferencedShared"));

        Assert.IsType<AvroSchemaReference>(referencedField.Type);
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
