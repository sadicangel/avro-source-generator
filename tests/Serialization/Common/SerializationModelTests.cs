using AvroSourceGenerator.IntegrationTests.Schemas;
using Xunit;

namespace AvroSourceGenerator.IntegrationTests;

public sealed class SerializationTests
{
    [Fact]
    public void Primitives_survive_binary_serialization() => Verify(SerializationCases.CreatePrimitives());

    [Fact]
    public void Records_survive_binary_serialization() => Verify(SerializationCases.CreateRecords());

    [Fact]
    public void Enums_survive_binary_serialization() => Verify(SerializationCases.CreateEnums());

    [Fact]
    public void Logical_types_survive_binary_serialization() => Verify(SerializationCases.CreateLogicalTypes());

    [Fact]
    public void Imported_types_survive_binary_serialization() => Verify(SerializationCases.CreateImports());

    [Fact]
    public void Java_extensions_survive_binary_serialization() => Verify(SerializationCases.CreateJavaExtensions());

    [Fact]
    public void Arrays_survive_binary_serialization() => Verify(SerializationCases.CreateCollections() with { intMap = [] });

    [Fact]
    public void Maps_survive_binary_serialization() => Verify(SerializationCases.CreateCollections() with { stringList = [] });

    [Fact]
    public void Collections_survive_binary_serialization() => Verify(SerializationCases.CreateCollections());

    [Fact(Skip = "The existing fixture contains an object cycle; Avro represents recursive values as trees, not object identity cycles.")]
    public void Cyclic_object_graph_survives_binary_serialization()
    {
        var root = new SelfReference { items = [], lookup = [], next = null };
        root.lookup["self"] = root;
        Verify(root);
    }

    private static void Verify<T>(T expected)
        => SerializationAssertions.Equal(expected, BinarySerialization.Roundtrip(expected));
}
