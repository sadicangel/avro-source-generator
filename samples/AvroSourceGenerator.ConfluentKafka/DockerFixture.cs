using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace AvroSourceGenerator.ConfluentKafka;

public sealed class DockerFixture : IAsyncDisposable
{
    public INetwork Network { get; }
    public IContainer Kafka { get; }
    public IContainer Zookeeper { get; }
    public IContainer SchemaRegistry { get; }

    public DockerFixture()
    {
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString())
            .Build();

        Kafka = new ContainerBuilder()
            .WithImage("confluentinc/cp-kafka:latest")
            .WithName("broker2")
            .WithPortBinding(9092)
            .WithNetworkAliases("broker2")
            .WithNetwork(Network)
            .WithEnvironment(new Dictionary<string, string>
            {
                ["KAFKA_BROKER_ID"] = "1",
                ["KAFKA_ZOOKEEPER_CONNECT"] = "zookeeper:2181",
                ["KAFKA_LISTENER_SECURITY_PROTOCOL_MAP"] = "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT",
                ["KAFKA_INTER_BROKER_LISTENER_NAME"] = "PLAINTEXT",
                ["KAFKA_ADVERTISED_LISTENERS"] = "PLAINTEXT://broker2:29092,PLAINTEXT_HOST://localhost:9092",
                ["KAFKA_AUTO_CREATE_TOPICS_ENABLE"] = "true",
                ["KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR"] = "1",
                ["KAFKA_TRANSACTION_STATE_LOG_MIN_ISR"] = "1",
                ["KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR"] = "1",
                ["KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS"] = "100",
            })
            .Build();

        Zookeeper = new ContainerBuilder()
            .WithImage("confluentinc/cp-zookeeper:latest")
            .WithName("zookeeper")
            .WithExposedPort(2181)
            .WithNetworkAliases("zookeeper")
            .WithNetwork(Network)
            .WithEnvironment("ZOOKEEPER_CLIENT_PORT", "2181")
            .Build();

        SchemaRegistry = new ContainerBuilder()
            .WithImage("confluentinc/cp-schema-registry:latest")
            .WithName("schema-registry")
            .WithExposedPort(8081)
            .WithPortBinding(8081, true)
            .WithNetworkAliases("schema-registry")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8081))
            .WithNetwork(Network)
            .WithEnvironment(new Dictionary<string, string>
            {
                ["SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS"] = "broker2:29092",
                ["SCHEMA_REGISTRY_HOST_NAME"] = "schema-registry",
                ["SCHEMA_REGISTRY_HOST_LISTENERS"] = "http://0.0.0.0:8081",
            })
            .Build();
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        await Network.CreateAsync(cancellationToken);
        await Zookeeper.StartAsync(cancellationToken);
        await Kafka.StartAsync(cancellationToken);
        await SchemaRegistry.StartAsync(cancellationToken);
    }

    public async ValueTask StopAsync() => await Task.WhenAll(
        SchemaRegistry.StopAsync(),
        Kafka.StopAsync(),
        Zookeeper.StopAsync(),
        Network.DeleteAsync());

    public ValueTask DisposeAsync() => StopAsync();

    public async Task CreateTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        var createTopicResult = await Kafka.ExecAsync([
            "kafka-topics","--create",
            "--topic", topicName,
            "--partitions", "1",
            "--replication-factor", "1",
            "--bootstrap-server", "localhost:9092"
        ], cancellationToken);

        if (!string.IsNullOrEmpty(createTopicResult.Stderr))
            throw new InvalidOperationException($"Failed to create topic: {createTopicResult.Stderr}");
    }

    public async Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        var deleteTopicResult = await Kafka.ExecAsync([
            "kafka-topics", "--delete",
            "--topic", topicName,
            "--bootstrap-server", "localhost:9092"
        ], cancellationToken);

        if (!string.IsNullOrEmpty(deleteTopicResult.Stderr))
            throw new InvalidOperationException($"Failed to delete topic: {deleteTopicResult.Stderr}");
    }

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
}
