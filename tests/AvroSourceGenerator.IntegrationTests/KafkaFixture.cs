using System.Globalization;
using System.Text.Json;
using Avro;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using DotNet.Testcontainers.Builders;
using Transaction = com.example.finance.Transaction;

namespace KafkaTestContainersTests;

public class KafkaTestingContainerWithSchemaRegistry
{
    private readonly ITestOutputHelper _testOutputHelper;

    public KafkaTestingContainerWithSchemaRegistry(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private const string ZookeeperPort = "2181";

    [Fact]
    public async Task CanIRun_Kafka_InAGenericTestcontainer_WithSchemaRegistry()
    {
        var networkName = Guid.NewGuid().ToString();
        var testcontainersNetworkBuilder = new NetworkBuilder()
            .WithName(networkName);

        var network = testcontainersNetworkBuilder.Build();

        _testOutputHelper.WriteLine("Starting network");
        await network.CreateAsync(TestContext.Current.CancellationToken);
        _testOutputHelper.WriteLine("Network started");

        var zookeeper = new ContainerBuilder()
            .WithImage("confluentinc/cp-zookeeper:latest")
            .WithName("zookeeper")
            .WithExposedPort(2181)
            .WithNetworkAliases("zookeeper")
            .WithNetwork(network)
            .WithEnvironment("ZOOKEEPER_CLIENT_PORT", ZookeeperPort.ToString(CultureInfo.InvariantCulture))
            .Build();

        await zookeeper.StartAsync(TestContext.Current.CancellationToken);

        var kafka = new ContainerBuilder()
            .WithImage("confluentinc/cp-kafka:latest")
            .WithName("broker2")
            .WithPortBinding(9092)
            .WithNetworkAliases("broker2")
            .WithNetwork(network)
            .WithEnvironment("KAFKA_BROKER_ID", "1")
            .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", "zookeeper:2181")
            .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT")
            .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
            .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://broker2:29092,PLAINTEXT_HOST://localhost:9092")
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "100")
            .Build();

        await kafka.StartAsync(TestContext.Current.CancellationToken);

        var schemaRegistryContainer = new ContainerBuilder()
            .WithImage("confluentinc/cp-schema-registry:latest")
            .WithName("schema-registry")
            .WithExposedPort(8081)
            .WithPortBinding(8081, true)
            .WithNetworkAliases("schema-registry")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8081))
            .WithNetwork(network)
            .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "broker2:29092")
            .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
            .WithEnvironment("SCHEMA_REGISTRY_HOST_LISTENERS", "http://0.0.0.0:8081")
            .Build();


        await schemaRegistryContainer.StartAsync(TestContext.Current.CancellationToken);

        _testOutputHelper.WriteLine("schemaRegistry started");

        var schemaRegistryConfig = new SchemaRegistryConfig
        {
            Url = $"{schemaRegistryContainer.Hostname}:{schemaRegistryContainer.GetMappedPublicPort(8081)}",
        };

        using var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = $"{kafka.Hostname}:{kafka.GetMappedPublicPort(9092)}",
        };

        using var producer = new ProducerBuilder<string, Transaction>(producerConfig)
            .SetValueSerializer(new AvroSerializer<Transaction>(schemaRegistry).AsSyncOverAsync())
            .Build();

        _testOutputHelper.WriteLine("producer built");

        var expected = new Transaction
        {
            id = Guid.NewGuid(),
            amount = new AvroDecimal(123.45m),
            currency = "USD",
            timestamp = DateTime.UtcNow.Date.Date,
            status = com.example.finance.TransactionStatus.COMPLETED,
            recipientId = "123456",
            metadata = new Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" },
                },
            signature = new com.example.finance.Signature(),
            legacyId = "abc123",
        };

        var deliveryResult = await producer.ProduceAsync("test", new Message<string, Transaction>
        {
            Key = "test",
            Value = expected,
        }, TestContext.Current.CancellationToken);
        producer.Flush(TimeSpan.FromSeconds(1));

        Assert.Equal(PersistenceStatus.Persisted, deliveryResult.Status);

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = $"{kafka.Hostname}:{kafka.GetMappedPublicPort(9092)}",
            GroupId = "test-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<string, Transaction>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<Transaction>(schemaRegistry).AsSyncOverAsync())
            .Build();

        consumer.Subscribe("test");

        //consumer.Assign(new TopicPartitionOffset("test", new Partition(0), new Offset(2)));

        var result = consumer.Consume(TimeSpan.FromSeconds(5));
        Assert.NotNull(result?.Message?.Value);

        var actual = result.Message.Value;

        Assert.Equal(expected, actual, new JsonEqualityComparer<Transaction>());

    }
}

file sealed class JsonEqualityComparer<T> : IEqualityComparer<T>
{
    private static readonly JsonSerializerOptions s_options = new() { WriteIndented = true };

    public bool Equals(T? x, T? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;

        var xJson = JsonSerializer.Serialize(x, s_options);
        var yJson = JsonSerializer.Serialize(y, s_options);

        return xJson == yJson;
    }
    public int GetHashCode(T obj) => JsonSerializer.Serialize(obj, s_options).GetHashCode();
}
