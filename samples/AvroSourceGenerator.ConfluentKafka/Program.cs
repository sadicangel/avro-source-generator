using AvroSourceGenerator.ConfluentKafka;
using Confluent.Kafka;

Console.WriteLine("Starting Docker fixture...");
await using var fixture = new DockerFixture();
await fixture.StartAsync();
Console.WriteLine("Docker fixture started.");

var topicName = "test-topic";
Console.WriteLine($"Creating topic: {topicName}...");
await fixture.CreateTopicAsync(topicName);
Console.WriteLine($"Topic '{topicName}' created.");

Console.WriteLine("Creating schema registry client...");
using var registry = fixture.CreateSchemaRegistryClient();
Console.WriteLine("Schema registry client created.");

Console.WriteLine("Creating order to produce...");
var producedOrder = new Order
{
    OrderId = "12345",
    Customer = new Customer
    {
        CustomerId = "C001",
        Name = "John Doe",
        Email = "john.doe@example.com"
    },
    Items =
    [
        new OrderItem
        {
            ProductId = "P001",
            Quantity = 2,
            Price = 19.99
        },
        new OrderItem
        {
            ProductId = "P002",
            Quantity = 1,
            Price = 49.99
        }
    ],
    Status = OrderStatus.Pending,
    OrderDate = DateTime.UtcNow.Date,
    LastUpdated = DateTime.UtcNow,
    Notes = "This is a sample order."
};
Console.WriteLine($"Order created: {producedOrder.OrderId}");

Console.WriteLine("Creating producer...");
using var producer = fixture.CreateProducer<Order>(registry);
Console.WriteLine("Producer created. Producing message...");
var deliveryResult = await producer.ProduceAsync(
    topicName,
    new Message<string, Order>
    {
        Key = Guid.NewGuid().ToString(),
        Value = producedOrder,
    });
if (deliveryResult.Status != PersistenceStatus.Persisted)
    throw new InvalidOperationException($"Failed to produce message: {deliveryResult.Status}");
Console.WriteLine($"Message produced successfully to topic '{topicName}'.");

Console.WriteLine("Creating consumer...");
using var consumer = fixture.CreateConsumer<Order>(registry);
Console.WriteLine("Consumer created. Subscribing to topic...");
consumer.Subscribe(topicName);
Console.WriteLine($"Subscribed to topic '{topicName}'. Consuming message...");
var consumedOrder = consumer.Consume(TimeSpan.FromSeconds(5))?.Message?.Value
    ?? throw new InvalidOperationException("Failed to consume message");
Console.WriteLine($"Message consumed successfully. Order ID: {consumedOrder.OrderId}");

if (producedOrder.OrderId != consumedOrder.OrderId)
    throw new InvalidOperationException($"Order ID mismatch: produced {producedOrder.OrderId}, consumed {consumedOrder.OrderId}");
Console.WriteLine("Produced and consumed orders match.");

Console.WriteLine($"Deleting topic '{topicName}'...");
Console.WriteLine($"Topic '{topicName}' deleted successfully.");
