using AvroSourceGenerator.IntegrationTests.Schemas;
using Chr.Avro.Confluent;
using Chr.Avro.Serialization;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Schema = Confluent.SchemaRegistry.Schema;

namespace AvroSourceGenerator.IntegrationTests.Chr;

public static class DockerFixtureExtensions
{
    private static readonly Schema s_keySchema = new("\"string\"", SchemaType.Avro);

    extension(DockerFixture docker)
    {
        public ISchemaRegistryClient CreateSchemaRegistryClient(Action<SchemaRegistryConfig>? configure = null)
        {
            var config = new SchemaRegistryConfig
            {
                Url = $"{docker.SchemaRegistry.Hostname}:{docker.SchemaRegistry.GetMappedPublicPort(8081)}",
            };
            configure?.Invoke(config);
            return new CachedSchemaRegistryClient(config);
        }

        public IProducer<string, T> CreateProducer<T>(ISchemaRegistryClient schemaRegistryClient, Action<ProducerConfig>? configure = null)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = $"{docker.Kafka.Hostname}:{docker.Kafka.GetMappedPublicPort(9092)}",
                MessageTimeoutMs = 10000,
            };
            configure?.Invoke(config);

            var serializerBuilder = new BinarySerializerBuilder(
                BinarySerializerBuilder.CreateDefaultCaseBuilders()
                    .Prepend(builder => new NotificationContentVariantSerializerBuilderCase(builder)));

            return new ProducerBuilder<string, T>(config)
                .SetAvroKeySerializer(schemaRegistryClient)
                .SetValueSerializer(
                    new AsyncSchemaRegistrySerializer<T>(
                        registryClient: schemaRegistryClient,
                        serializerBuilder: serializerBuilder).AsSyncOverAsync())
                .Build();
        }

        public IConsumer<string, T> CreateConsumer<T>(ISchemaRegistryClient schemaRegistryClient, Action<ConsumerConfig>? configure = null)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = $"{docker.Kafka.Hostname}:{docker.Kafka.GetMappedPublicPort(9092)}",
                GroupId = Guid.NewGuid().ToString(),
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };
            configure?.Invoke(config);

            var deserializerBuilder = new BinaryDeserializerBuilder(
                BinaryDeserializerBuilder.CreateDefaultCaseBuilders()
                    .Prepend(builder => new NotificationContentVariantDeserializerBuilderCase(builder)));

            return new ConsumerBuilder<string, T>(config)
                .SetAvroKeyDeserializer(schemaRegistryClient)
                .SetValueDeserializer(
                    new AsyncSchemaRegistryDeserializer<T>(
                        schemaRegistryClient,
                        deserializerBuilder).AsSyncOverAsync())
                .Build();
        }

        public async Task<T> RoundtripAsync<T>(T message, Schema valueSchema, CancellationToken cancellationToken = default)
        {
            var topicName = Guid.NewGuid().ToString();

            await docker.CreateTopicAsync(topicName, cancellationToken: cancellationToken);

            using var registry = docker.CreateSchemaRegistryClient();
            using var producer = docker.CreateProducer<T>(registry);
            using var consumer = docker.CreateConsumer<T>(registry);

            await registry.RegisterSchemaAsync($"{topicName}-key", s_keySchema);
            await registry.RegisterSchemaAsync($"{topicName}-value", valueSchema);

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
            message = consumer.Consume(TimeSpan.FromSeconds(10)).Message.Value
                ?? throw new InvalidOperationException("Failed to consume message");

            await docker.DeleteTopicAsync(topicName, cancellationToken: cancellationToken);

            return message;
        }
    }
}
