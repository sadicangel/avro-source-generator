using System.Text.Json;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaRegistrySubjectTests
{
    [Fact]
    public void RegisterSubject_FieldDefault_RemainsAccessibleAfterSubjectPayloadDocumentIsDisposed()
    {
        var subject = Parse(
            """
            {
              "schemaType": "AVRO",
              "schema": "{\"type\":\"record\",\"name\":\"OrderCreated\",\"namespace\":\"Demo\",\"fields\":[{\"name\":\"Notes\",\"type\":[\"null\",\"string\"],\"default\":null}]}"
            }
            """);

        var registry = new SchemaRegistry(SchemaRegistryOptions.Default);
        registry.RegisterSubject(subject);

        var record = Assert.IsType<RecordSchema>(Assert.Single(registry.Schemas.Values));
        var field = Assert.Single(record.Fields);

        Assert.NotNull(field.DefaultJson);
        Assert.Equal(JsonValueKind.Null, field.DefaultJson!.Value.ValueKind);
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
