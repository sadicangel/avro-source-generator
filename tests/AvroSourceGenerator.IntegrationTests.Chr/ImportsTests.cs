using AvroSourceGenerator.IntegrationTests.Schemas;

using Chr.Avro.Abstract;
using Chr.Avro.Representation;
using Confluent.SchemaRegistry;
using ConfluentSchema = Confluent.SchemaRegistry.Schema;

namespace AvroSourceGenerator.IntegrationTests.Chr;

public class ImportsTests(DockerFixture dockerFixture)
{
    [Fact]
    public async Task Imported_schema_types_remain_unchanged_after_roundtrip_to_kafka()
    {
        var item = new NestedType { displayName = "Imported" };
        var expected = new ImportedEnvelope { Item = item };
        var schema = new SchemaBuilder().BuildSchema<ImportedEnvelope>();
        var valueSchema = new ConfluentSchema(
            new JsonSchemaWriter().Write(schema),
            SchemaType.Avro);

        var actual = await dockerFixture.RoundtripAsync(
            expected,
            valueSchema,
            TestContext.Current.CancellationToken);

        Assert.EqualAsJson(expected, actual);
    }
}
