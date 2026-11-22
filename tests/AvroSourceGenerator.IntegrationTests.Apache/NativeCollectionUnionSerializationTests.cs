using Avro;
using Avro.Generic;
using Avro.IO;
using Avro.Specific;
using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

public sealed class NativeCollectionUnionSerializationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Specific_readers_round_trip_native_unions_in_collections(bool useDatumReader, bool reuseRecord)
    {
        var expected = new NativeCollectionUnions
        {
            Items = ["first", 42],
            Values = new() { ["missing"] = null, ["count"] = 123L, ["enabled"] = true },
            Choice = new List<NativeCollectionUnionsChoiceItemUnion?> { true, 2.5, null },
        };
        var schema = (RecordSchema)((ISpecificRecord)expected).Schema;
        var raw = new GenericRecord(schema);
        raw.Add("Items", new object[] { "first", 42 });
        raw.Add("Values", new Dictionary<string, object?> { ["missing"] = null, ["count"] = 123L, ["enabled"] = true });
        raw.Add("Choice", new object?[] { true, 2.5, null });
        raw.Add("Nested", null);

        using var genericBytes = new MemoryStream();
        new GenericDatumWriter<GenericRecord>(schema).Write(raw, new BinaryEncoder(genericBytes));
        using var specificBytes = new MemoryStream();
        new SpecificDatumWriter<NativeCollectionUnions>(schema).Write(expected, new BinaryEncoder(specificBytes));
        Assert.Equal(genericBytes.ToArray(), specificBytes.ToArray());
        using var alternateBytes = new MemoryStream();
        new SpecificWriter<NativeCollectionUnions>(schema).Write(expected, new BinaryEncoder(alternateBytes));
        Assert.Equal(genericBytes.ToArray(), alternateBytes.ToArray());

        var reuse = reuseRecord ? new NativeCollectionUnions
        {
            Items = ["old"],
            Values = new() { ["old"] = false },
            Choice = new List<NativeCollectionUnionsChoiceItemUnion?> { false, 3.5 },
        } : null;
        specificBytes.Position = 0;
        var decoder = new BinaryDecoder(specificBytes);
        var actual = useDatumReader
            ? new SpecificDatumReader<NativeCollectionUnions>(schema, schema).Read(reuse!, decoder)
            : new SpecificReader<NativeCollectionUnions>(schema, schema).Read(reuse!, decoder);

        if (reuseRecord)
            Assert.Same(reuse, actual);
        Assert.Equal(expected.Items, actual.Items);
        Assert.Equal(expected.Values, actual.Values);
        Assert.Equal(Assert.IsType<List<NativeCollectionUnionsChoiceItemUnion?>>(expected.Choice?.Value),
            Assert.IsType<List<NativeCollectionUnionsChoiceItemUnion?>>(actual.Choice?.Value));
    }

    [Fact]
    public void Get_and_Put_convert_native_unions_at_each_collection_level()
    {
        var model = new NativeCollectionUnions { Items = [], Values = new() };
        var record = (ISpecificRecord)model;
        var field = ((RecordSchema)record.Schema).Fields.Single(field => field.Name == "Nested");
        var raw = new[] { new Dictionary<string, object> { ["number"] = 5L, ["items"] = new object[] { "text", 7 } } };

        record.Put(field.Pos, raw);

        var nested = Assert.Single(Assert.IsType<List<Dictionary<string, NativeCollectionUnionsNestedItemValueUnion>>>(model.Nested));
        Assert.Equal(5L, nested["number"].Value);
        var items = Assert.IsType<List<NativeCollectionUnionsNestedItemValueItemUnion>>(nested["items"].Value);
        Assert.Collection(items, item => Assert.Equal("text", item.Value), item => Assert.Equal(7, item.Value));

        var projected = Assert.Single(Assert.IsType<List<object?>>(record.Get(field.Pos)));
        var map = Assert.IsType<Dictionary<string, object?>>(projected);
        Assert.Equal(5L, map["number"]);
        Assert.Equal<object?>(["text", 7], Assert.IsType<List<object?>>(map["items"]));
    }
}
