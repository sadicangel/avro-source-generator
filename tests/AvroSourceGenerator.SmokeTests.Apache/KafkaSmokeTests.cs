using AvroSourceGenerator.IntegrationTests.Schemas;
using AvroSourceGenerator.SmokeTests;
using AvroSourceGenerator.SmokeTests.Apache;

namespace AvroSourceGenerator.IntegrationTests;

public sealed partial class KafkaSmokeTests
{
    private static Task<KafkaSmoke> RoundtripAsync(DockerFixture docker, KafkaSmoke expected, CancellationToken cancellationToken) =>
        docker.RoundtripAsync(expected, cancellationToken);
}
