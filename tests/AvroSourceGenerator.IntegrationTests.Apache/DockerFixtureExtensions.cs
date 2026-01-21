using Avro.Specific;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace AvroSourceGenerator.IntegrationTests.Apache;

public static class DockerFixtureExtensions
{
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
                BootstrapServers = $"{docker.Kafka.Hostname}:{docker.Kafka.GetMappedPublicPort(9092)}",
                GroupId = Guid.NewGuid().ToString(),
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };
            configure?.Invoke(config);
            return new ConsumerBuilder<string, T>(config)
                .SetValueDeserializer(new AvroDeserializer<T>(schemaRegistryClient).AsSyncOverAsync())
                .Build();
        }

        public async Task<T> RoundtripAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, ISpecificRecord
        {
            var topicName = Guid.NewGuid().ToString();

            await docker.CreateTopicAsync(topicName, cancellationToken: cancellationToken);

            using var registry = docker.CreateSchemaRegistryClient();
            using var producer = docker.CreateProducer<T>(registry);
            using var consumer = docker.CreateConsumer<T>(registry);

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

            await docker.DeleteTopicAsync(topicName, cancellationToken: cancellationToken);

            return message;
        }
    }
}
