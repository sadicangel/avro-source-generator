using Avro;
using com.example.finance;

var transaction = new Transaction
{
    id = Guid.NewGuid(),
    amount = new AvroDecimal(123.45m),
    currency = "USD",
    timestamp = DateTime.UtcNow,
    status = TransactionStatus.COMPLETED,
    recipientId = "123456",
    metadata = new Dictionary<string, string>
    {
        { "key1", "value1" },
        { "key2", "value2" },
    },
    signature = new Signature(),
    legacyId = "abc123",
};

Random.Shared.NextBytes(transaction.signature.Value);

Console.WriteLine(transaction);
