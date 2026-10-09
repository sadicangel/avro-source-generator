using AvroSourceGenerator.Compiler;
using Chr.Avro.Representation;
using Xunit;

namespace AvroSourceGenerator.IntegrationTests;

internal static partial class BinarySerialization
{
    public static T Roundtrip<T>(T expected)
    {
        var schema = new JsonSchemaReader().Read(SchemaFixtures.ReadJson(typeof(T), GenerationTarget.Chr));
        var serialize = CreateSerializerBuilder().BuildDelegate<T>(schema);
        var deserialize = CreateDeserializerBuilder().BuildDelegate<T>(schema);
        using var stream = new MemoryStream();
        serialize(expected, new global::Chr.Avro.Serialization.BinaryWriter(stream));
        var reader = new global::Chr.Avro.Serialization.BinaryReader(stream.ToArray());
        var actual = deserialize(ref reader);
        Assert.Equal(stream.Length, reader.Index);
        return actual;
    }
}
