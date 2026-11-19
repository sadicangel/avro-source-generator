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

    [Fact]
    public void Recursive_next_chain_survives_binary_serialization()
    {
        var tail = CreateLeaf();
        var middle = new SelfReference { items = [], lookup = [], next = tail };
        var root = new SelfReference { items = [], lookup = [], next = middle };

        Verify(root);
    }

    [Fact]
    public void Recursive_items_survive_binary_serialization()
    {
        var child = new SelfReference { items = [CreateLeaf()], lookup = [], next = null };
        var root = new SelfReference { items = [child, CreateLeaf()], lookup = [], next = null };

        Verify(root);
    }

    [Fact]
    public void Recursive_lookup_survives_binary_serialization()
    {
        var descendant = new SelfReference { items = [CreateLeaf()], lookup = [], next = null };
        var child = new SelfReference { items = [], lookup = [], next = descendant };
        var root = new SelfReference
        {
            items = [],
            lookup = new Dictionary<string, SelfReference>
            {
                ["branch"] = child,
                ["leaf"] = CreateLeaf(),
            },
            next = null,
        };

        Verify(root);
    }

    private static SelfReference CreateLeaf() => new SelfReference { items = [], lookup = [], next = null };

    private static void Verify<T>(T expected)
        => SerializationAssertions.Equal(expected, BinarySerialization.Roundtrip(expected));
}
