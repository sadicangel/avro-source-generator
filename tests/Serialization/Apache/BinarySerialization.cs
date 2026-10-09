using Avro.IO;
using Avro.Specific;
using Xunit;

namespace AvroSourceGenerator.IntegrationTests;

internal static class BinarySerialization
{
    public static T Roundtrip<T>(T expected)
    {
        var schema = Assert.IsAssignableFrom<ISpecificRecord>(expected).Schema;
        using var stream = new MemoryStream();
        new SpecificDatumWriter<T>(schema).Write(expected, new BinaryEncoder(stream));
        stream.Position = 0;
        var actual = new SpecificDatumReader<T>(schema, schema).Read(default!, new BinaryDecoder(stream));
        Assert.Equal(stream.Length, stream.Position);
        return actual;
    }
}
