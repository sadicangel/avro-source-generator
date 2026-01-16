using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests.Chr;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var expected = new Primitives
        {
            intField = 42,
            longField = 42L,
            floatField = 42.42f,
            doubleField = 42.42,
            boolField = true,
            stringField = "Hello, Avro!",
            bytesField = "Hello, Avro!"u8.ToArray(),
            nullableInt1 = 42,
            nullableLong1 = 42L,
            nullableFloat1 = 42.42f,
            nullableDouble1 = 42.42,
            nullableBool1 = true,
            nullableString1 = "Hello, Avro!",
            nullableBytes1 = "Hello, Avro!"u8.ToArray(),
            nullableInt2 = null,
            nullableLong2 = null,
            nullableFloat2 = null,
            nullableDouble2 = null,
            nullableBool2 = null,
            nullableString2 = null,
            nullableBytes2 = null,
        };

        Assert.Skip("TODO");
    }
}
