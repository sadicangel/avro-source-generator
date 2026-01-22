using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Xunit;

namespace AvroSourceGenerator.IntegrationTests;

public sealed class DockerFixture : IAsyncLifetime
{
    public INetwork Network { get; }
    public IContainer Kafka { get; }
    public IContainer SchemaRegistry { get; }

    public DockerFixture()
    {
        Network = new NetworkBuilder()
            .WithName($"network-{Guid.NewGuid()}")
            .Build();

        var kafkaName = $"kraft-kafka-{Guid.NewGuid()}";
        var kafkaPort = GetFreeTcpPort();
        Kafka = new ContainerBuilder()
            .WithImage("confluentinc/cp-kafka:7.6.1")
            .WithName(kafkaName)
            .WithPortBinding(kafkaPort, 9092)
            .WithNetworkAliases(kafkaName)
            .WithNetwork(Network)
            .WithEnvironment(
                new Dictionary<string, string>
                {
                    ["KAFKA_NODE_ID"] = "1",
                    ["KAFKA_LISTENER_SECURITY_PROTOCOL_MAP"] = "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,CONTROLLER:PLAINTEXT",
                    ["KAFKA_LISTENERS"] = "PLAINTEXT://0.0.0.0:29092,PLAINTEXT_HOST://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093",
                    ["KAFKA_ADVERTISED_LISTENERS"] = $"PLAINTEXT://{kafkaName}:29092,PLAINTEXT_HOST://localhost:{kafkaPort}",
                    ["KAFKA_PROCESS_ROLES"] = "broker,controller",
                    ["KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR"] = "1",
                    ["KAFKA_CONTROLLER_QUORUM_VOTERS"] = $"1@{kafkaName}:9093",
                    ["KAFKA_INTER_BROKER_LISTENER_NAME"] = "PLAINTEXT",
                    ["KAFKA_CONTROLLER_LISTENER_NAMES"] = "CONTROLLER",
                    ["CLUSTER_ID"] = GenerateClusterId(),
                })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(29092))
            .Build();

        var schemaRegistryName = $"schema-registry-{Guid.NewGuid()}";
        SchemaRegistry = new ContainerBuilder()
            .WithImage("confluentinc/cp-schema-registry:7.6.0")
            .WithName(schemaRegistryName)
            .WithPortBinding(8081, assignRandomHostPort: true)
            .WithNetworkAliases(schemaRegistryName)
            .WithNetwork(Network)
            .WithEnvironment(
                new Dictionary<string, string>
                {
                    ["SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS"] = $"{kafkaName}:29092",
                    ["SCHEMA_REGISTRY_HOST_NAME"] = schemaRegistryName,
                    ["SCHEMA_REGISTRY_HOST_LISTENERS"] = "http://0.0.0.0:8081",
                    ["SCHEMA_REGISTRY_DEBUG"] = "true",
                })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8081))
            .Build();

        static string GenerateClusterId()
        {
            Span<char> clusterId = stackalloc char[22];
            RandomNumberGenerator.GetItems("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", clusterId);
            return clusterId.ToString();
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        listener.Stop();
        return port;
    }

    public async ValueTask InitializeAsync()
    {
        await Network.CreateAsync(TestContext.Current.CancellationToken);
        await Kafka.StartAsync(TestContext.Current.CancellationToken);
        await SchemaRegistry.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await Task.WhenAll(
        SchemaRegistry.StopAsync(TestContext.Current.CancellationToken),
        Kafka.StopAsync(TestContext.Current.CancellationToken),
        Network.DeleteAsync(TestContext.Current.CancellationToken));

    public async Task CreateTopicAsync(string topicName, int partitionCount = 1, int replicationFactor = 1, CancellationToken cancellationToken = default)
    {
        var createTopicResult = await Kafka.ExecAsync(
            [
                "kafka-topics", "--create",
                "--topic", topicName,
                "--partitions", partitionCount.ToString(),
                "--replication-factor", replicationFactor.ToString(),
                "--bootstrap-server", "0.0.0.0:29092"
            ],
            cancellationToken);

        if (!string.IsNullOrEmpty(createTopicResult.Stderr))
            throw new InvalidOperationException($"Failed to create topic '{topicName}': {createTopicResult.Stderr}");
    }

    public async Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        var deleteTopicResult = await Kafka.ExecAsync(
            [
                "kafka-topics", "--delete",
                "--topic", topicName,
                "--bootstrap-server", "0.0.0.0:29092"
            ],
            cancellationToken);

        if (!string.IsNullOrEmpty(deleteTopicResult.Stderr))
            throw new InvalidOperationException($"Failed to delete topic '{topicName}': {deleteTopicResult.Stderr}");
    }
}
