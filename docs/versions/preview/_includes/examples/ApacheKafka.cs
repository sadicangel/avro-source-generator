using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using example;

var brokers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var registryUrl = Environment.GetEnvironmentVariable("SCHEMA_REGISTRY_URL") ?? "http://localhost:8081";
const string topic = "users";

using var registry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = registryUrl });
using var producer = new ProducerBuilder<string, User>(new ProducerConfig
    {
        BootstrapServers = brokers,
        MessageTimeoutMs = 15000,
    })
    .SetKeySerializer(Serializers.Utf8)
    .SetValueSerializer(new AvroSerializer<User>(registry).AsSyncOverAsync())
    .Build();

using var consumer = new ConsumerBuilder<string, User>(new ConsumerConfig
    {
        BootstrapServers = brokers,
        GroupId = $"user-demo-{Guid.NewGuid():N}",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false,
    })
    .SetKeyDeserializer(Deserializers.Utf8)
    .SetValueDeserializer(new AvroDeserializer<User>(registry).AsSyncOverAsync())
    .Build();

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
var key = Guid.NewGuid().ToString("N");
var original = new User { Name = "Ada", Email = "ada@example.com" };

await producer.ProduceAsync(topic, new Message<string, User> { Key = key, Value = original }, timeout.Token);
consumer.Subscribe(topic);

User restored;
while (true)
{
    var received = consumer.Consume(timeout.Token);
    if (received.Message.Key != key) continue;
    restored = received.Message.Value;
    break;
}

consumer.Close();
if (restored.Name != original.Name || restored.Email != original.Email)
    throw new InvalidOperationException("The consumed User differs from the produced User.");

Console.WriteLine($"Round trip: {restored.Name} <{restored.Email}>");
