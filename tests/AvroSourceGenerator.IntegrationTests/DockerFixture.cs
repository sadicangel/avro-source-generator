using Avro.Specific;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace AvroSourceGenerator.IntegrationTests;

public sealed class DockerFixture : IAsyncLifetime
{
    public INetwork Network { get; }
    public IContainer Kafka { get; }
    public IContainer SchemaRegistry { get; }

    public DockerFixture()
    {
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString())
            .Build();

        Kafka = new ContainerBuilder()
            .WithImage("confluentinc/cp-kafka:7.6.1")
            .WithName("kraft-kafka")
            .WithPortBinding(9092)
            .WithNetworkAliases("kraft-kafka")
            .WithNetwork(Network)
            .WithEnvironment(
                new Dictionary<string, string>
                {
                    ["KAFKA_NODE_ID"] = "1",
                    ["KAFKA_LISTENER_SECURITY_PROTOCOL_MAP"] = "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,CONTROLLER:PLAINTEXT",
                    ["KAFKA_LISTENERS"] = "PLAINTEXT://0.0.0.0:29092,PLAINTEXT_HOST://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093",
                    ["KAFKA_ADVERTISED_LISTENERS"] = "PLAINTEXT://kraft-kafka:29092,PLAINTEXT_HOST://localhost:9092",
                    ["KAFKA_PROCESS_ROLES"] = "broker,controller",
                    ["KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR"] = "1",
                    ["KAFKA_CONTROLLER_QUORUM_VOTERS"] = "1@kraft-kafka:9093",
                    ["KAFKA_INTER_BROKER_LISTENER_NAME"] = "PLAINTEXT",
                    ["KAFKA_CONTROLLER_LISTENER_NAMES"] = "CONTROLLER",
                    ["CLUSTER_ID"] = "MkU3OEVBNTcwNTJENDM2Qk"
                })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(29092))
            .Build();

        SchemaRegistry = new ContainerBuilder()
            .WithImage("confluentinc/cp-schema-registry:7.6.0")
            .WithName("schema-registry")
            .WithPortBinding(8081)
            .WithNetworkAliases("schema-registry")
            .WithNetwork(Network)
            .WithEnvironment(
                new Dictionary<string, string>
                {
                    ["SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS"] = "kraft-kafka:29092",
                    ["SCHEMA_REGISTRY_HOST_NAME"] = "schema-registry",
                    ["SCHEMA_REGISTRY_HOST_LISTENERS"] = "http://0.0.0.0:8081",
                    ["SCHEMA_REGISTRY_DEBUG"] = "true",
                })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8081))
            .Build();
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

    public ISchemaRegistryClient CreateSchemaRegistryClient(Action<SchemaRegistryConfig>? configure = null)
    {
        var config = new SchemaRegistryConfig
        {
            Url = $"{SchemaRegistry.Hostname}:{SchemaRegistry.GetMappedPublicPort(8081)}",
        };
        configure?.Invoke(config);
        return new CachedSchemaRegistryClient(config);
    }

    public IProducer<string, T> CreateProducer<T>(ISchemaRegistryClient schemaRegistryClient, Action<ProducerConfig>? configure = null)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = $"{Kafka.Hostname}:{Kafka.GetMappedPublicPort(9092)}",
            MessageTimeoutMs = 10000
        };
        configure?.Invoke(config);
        return new ProducerBuilder<string, T>(config)
            .SetValueSerializer(new AvroSerializer<T>(schemaRegistryClient).AsSyncOverAsync())
            .Build();
    }

    public IConsumer<string, T> CreateConsumer<T>(ISchemaRegistryClient schemaRegistryClient, Action<ConsumerConfig>? configure = null)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = $"{Kafka.Hostname}:{Kafka.GetMappedPublicPort(9092)}",
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        configure?.Invoke(config);
        return new ConsumerBuilder<string, T>(config)
            .SetValueDeserializer(new AvroDeserializer<T>(schemaRegistryClient).AsSyncOverAsync())
            .Build();
    }

    public async Task CreateTopicAsync(string topicName, int partitionCount = 1, int replicationFactor = 1, CancellationToken cancellationToken = default)
    {
        var createTopicResult = await Kafka.ExecAsync(
            [
                "kafka-topics", "--create",
                "--topic", topicName,
                "--partitions", partitionCount.ToString(),
                "--replication-factor", replicationFactor.ToString(),
                "--bootstrap-server", "0.0.0.0:9092"
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
                "--bootstrap-server", "0.0.0.0:9092"
            ],
            cancellationToken);

        if (!string.IsNullOrEmpty(deleteTopicResult.Stderr))
            throw new InvalidOperationException($"Failed to delete topic '{topicName}': {deleteTopicResult.Stderr}");
    }

    public async Task<T> RoundtripAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class, ISpecificRecord
    {
        var topicName = Guid.NewGuid().ToString();

        await CreateTopicAsync(topicName, cancellationToken: cancellationToken);

        using var registry = CreateSchemaRegistryClient();
        using var producer = CreateProducer<T>(registry);
        using var consumer = CreateConsumer<T>(registry);

        var deliveryResult = await producer.ProduceAsync(
            topicName,
            new Message<string, T>
            {
                Key = Guid.NewGuid().ToString(),
                Value = message,
            },
            cancellationToken);

        if (deliveryResult.Status != PersistenceStatus.Persisted)
            throw new InvalidOperationException($"Failed to produce message: {deliveryResult.Status}");

        consumer.Subscribe(topicName);
        message = consumer.Consume(TimeSpan.FromSeconds(10))?.Message?.Value
            ?? throw new InvalidOperationException("Failed to consume message");

        await DeleteTopicAsync(topicName, cancellationToken: cancellationToken);

        return message;
    }
}
