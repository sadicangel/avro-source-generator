using AvroSourceGenerator.ChrAvroKafka;
using Confluent.Kafka;

const string topicName = "transactions";
var valueSubject = $"{topicName}-value";

Console.WriteLine("Starting Kafka and Schema Registry containers...");
await using var fixture = new DockerFixture();
await fixture.StartAsync();
Console.WriteLine("Containers are ready.");

Console.WriteLine($"Creating topic '{topicName}'...");
await fixture.CreateTopicAsync(topicName);
Console.WriteLine($"Topic '{topicName}' created.");

using var registry = fixture.CreateSchemaRegistryClient();

Console.WriteLine("Creating a sample transaction...");
var producedTransaction = CreateTransaction();
Console.WriteLine($"Transaction {producedTransaction.Id} created.");

Console.WriteLine("Creating producer...");
using var producer = await fixture.CreateProducerAsync<TransactionEvent>(registry, valueSubject);

Console.WriteLine("Producing message...");
var deliveryResult = await producer.ProduceAsync(
    topicName,
    new Message<string, TransactionEvent>
    {
        Key = producedTransaction.Id.ToString(),
        Value = producedTransaction,
    });

if (deliveryResult.Status != PersistenceStatus.Persisted)
{
    throw new InvalidOperationException($"Failed to produce message: {deliveryResult.Status}");
}

var registeredSchema = await registry.GetLatestSchemaAsync(valueSubject);
Console.WriteLine(
    $"Registered subject '{valueSubject}' at version {registeredSchema.Version} with schema id {registeredSchema.Id}.");

Console.WriteLine("Creating consumer...");
using var consumer = fixture.CreateConsumer<TransactionEvent>(registry);

Console.WriteLine("Subscribing and consuming...");
consumer.Subscribe(topicName);

var consumedTransaction = consumer.Consume(TimeSpan.FromSeconds(10))?.Message?.Value
    ?? throw new InvalidOperationException("Failed to consume the produced transaction.");

Console.WriteLine($"Consumed transaction {consumedTransaction.Id}.");

AssertEquivalent(producedTransaction, consumedTransaction);
Console.WriteLine("Produced and consumed transactions match.");

Console.WriteLine($"Deleting topic '{topicName}'...");
await fixture.DeleteTopicAsync(topicName);
Console.WriteLine("Sample completed successfully.");

static TransactionEvent CreateTransaction()
{
    var timestamp = DateTimeOffset.UtcNow;
    timestamp = timestamp.AddTicks(-(timestamp.Ticks % TimeSpan.TicksPerMillisecond));

    var signature = new byte[64];
    Random.Shared.NextBytes(signature);

    return new TransactionEvent
    {
        Id = Guid.NewGuid(),
        Amount = 123.45m,
        Currency = "EUR",
        Timestamp = timestamp,
        Status = TransactionStatus.COMPLETED,
        RecipientId = "merchant-42",
        Metadata = new Dictionary<string, string>
        {
            ["channel"] = "web",
            ["region"] = "eu-west",
        },
        Signature = signature,
        LegacyId = "legacy-0001",
    };
}

static void AssertEquivalent(TransactionEvent expected, TransactionEvent actual)
{
    if (expected.Id != actual.Id)
    {
        throw new InvalidOperationException($"Id mismatch: expected {expected.Id}, got {actual.Id}.");
    }

    if (expected.Amount != actual.Amount)
    {
        throw new InvalidOperationException($"Amount mismatch: expected {expected.Amount}, got {actual.Amount}.");
    }

    if (!string.Equals(expected.Currency, actual.Currency, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"Currency mismatch: expected '{expected.Currency}', got '{actual.Currency}'.");
    }

    if (expected.Timestamp != actual.Timestamp)
    {
        throw new InvalidOperationException(
            $"Timestamp mismatch: expected {expected.Timestamp:o}, got {actual.Timestamp:o}.");
    }

    if (expected.Status != actual.Status)
    {
        throw new InvalidOperationException($"Status mismatch: expected {expected.Status}, got {actual.Status}.");
    }

    if (!string.Equals(expected.RecipientId, actual.RecipientId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"RecipientId mismatch: expected '{expected.RecipientId}', got '{actual.RecipientId}'.");
    }

    if (!string.Equals(expected.LegacyId, actual.LegacyId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"LegacyId mismatch: expected '{expected.LegacyId}', got '{actual.LegacyId}'.");
    }

    if (!expected.Signature.SequenceEqual(actual.Signature))
    {
        throw new InvalidOperationException("Signature mismatch.");
    }

    if (expected.Metadata.Count != actual.Metadata.Count)
    {
        throw new InvalidOperationException(
            $"Metadata count mismatch: expected {expected.Metadata.Count}, got {actual.Metadata.Count}.");
    }

    foreach (var (key, value) in expected.Metadata)
    {
        if (!actual.Metadata.TryGetValue(key, out var actualValue))
        {
            throw new InvalidOperationException($"Metadata key '{key}' was not found in the consumed transaction.");
        }

        if (!string.Equals(value, actualValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Metadata mismatch for '{key}': expected '{value}', got '{actualValue}'.");
        }
    }
}
