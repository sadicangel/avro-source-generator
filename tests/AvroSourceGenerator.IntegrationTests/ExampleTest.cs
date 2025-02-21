using System.Text.Json;
using Avro;
using Transaction = com.example.finance.Transaction;

namespace AvroSourceGenerator.IntegrationTests;

public class ExampleTest(DockerFixture dockerFixture)
{

    [Fact]
    public async Task CanIRun_Kafka_InAGenericTestcontainer_WithSchemaRegistry()
    {
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

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

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
