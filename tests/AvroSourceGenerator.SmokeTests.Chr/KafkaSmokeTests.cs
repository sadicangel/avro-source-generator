using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.IntegrationTests.Schemas;
using AvroSourceGenerator.SmokeTests;
using AvroSourceGenerator.SmokeTests.Chr;
using Confluent.SchemaRegistry;

namespace AvroSourceGenerator.IntegrationTests;

public sealed partial class KafkaSmokeTests
{
    private static Task<KafkaSmoke> RoundtripAsync(DockerFixture docker, KafkaSmoke expected, CancellationToken cancellationToken)
    {
        var schema = new Schema(SchemaFixtures.ReadJson(typeof(KafkaSmoke), GenerationTarget.Chr), SchemaType.Avro);
        return docker.RoundtripAsync(expected, schema, cancellationToken);
    }
}
