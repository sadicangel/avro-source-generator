using System.Text;

namespace AvroSourceGenerator.IntegrationTests;

public class KafkaRoundTripTests(DockerFixture dockerFixture)
{
    // To avoid reference type comparison, use JSON.
    private static void AssertEqual<T>(T expected, T actual) =>
        Assert.Equal(expected, actual, new JsonEqualityComparer<T>());

    [Fact]
    public async Task Primitives_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new Primitives
        {
            intField = 42,
            longField = 42L,
            floatField = 42.42f,
            doubleField = 42.42,
            boolField = true,
            stringField = "Hello, Avro!",
            bytesField = Encoding.UTF8.GetBytes("Hello, Avro!"),

            nullableInt = 42,
            nullableLong = 42L,
            nullableFloat = 42.42f,
            nullableDouble = 42.42,
            nullableBool = true,
            nullableString = "Hello, Avro!",
            nullableBytes = Encoding.UTF8.GetBytes("Hello, Avro!"),
        };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        AssertEqual(expected, actual);
    }

    [Fact]
    public async Task Collections_remain_unchanged_after_roundtrip_to_kafka()
    {
        var expected = new Collections
        {
            stringList = ["Hello", "Avro", "!"],
            intMap = new Dictionary<string, int> { ["Hello"] = 1, ["Avro"] = 2, ["!"] = 3 },
        };

        var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);

        AssertEqual(expected, actual);
    }

    // TODO: Fix nested enums not being nullable when they should.
    //[Fact]
    //public async Task Enums_remain_unchanged_after_roundtrip_to_kafka()
    //{
    //    var expected = new Enums
    //    {
    //        status = Status.ACTIVE,
    //        nullableStatus = null,
    //    };
    //    var actual = await dockerFixture.RoundtripAsync(expected, TestContext.Current.CancellationToken);
    //    AssertEqual(expected, actual);
    //}
}
