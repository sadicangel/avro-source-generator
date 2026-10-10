using Avro;
using Avro.Generic;
using Avro.IO;
using Avro.Specific;
using AvroSourceGenerator.IntegrationTests.Schemas;
using Xunit;

namespace AvroSourceGenerator.IntegrationTests;

public sealed class NestedCollectionSerializationTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void Nested_collections_preserve_binary_values(string reader, string fieldName, int mode)
    {
        var schema = (RecordSchema)((ISpecificRecord)Activator.CreateInstance(typeof(NestedCollections))!).Schema;
        var expected = new GenericRecord(schema);

        foreach (var field in schema.Fields)
        {
            expected.Add(field.Name, null);
        }

        expected.Add(fieldName, Sample(fieldName, mode));

        using var original = new MemoryStream();
        new GenericDatumWriter<GenericRecord>(schema).Write(expected, new BinaryEncoder(original));
        original.Position = 0;

        NestedCollections actual;
        if (reader == "generic-put")
        {
            // Apache 1.12.2 cannot construct certain nested IDictionary types before Put is called.
            // A generic reader still exercises the generated conversion and specific writer for those shapes.
            var generic = new GenericDatumReader<GenericRecord>(schema, schema).Read(default!, new BinaryDecoder(original));
            actual = (NestedCollections)Activator.CreateInstance(typeof(NestedCollections))!;

            foreach (var field in schema.Fields)
            {
                ((ISpecificRecord)actual).Put(field.Pos, generic[field.Name]);
            }
        }
        else if (reader == "specific")
        {
            actual = new SpecificReader<NestedCollections>(schema, schema).Read(default!, new BinaryDecoder(original));
        }
        else
        {
            actual = new SpecificDatumReader<NestedCollections>(schema, schema).Read(default!, new BinaryDecoder(original));
        }

        Assert.Equal(original.Length, original.Position);

        using var encoded = new MemoryStream();
        new SpecificDatumWriter<NestedCollections>(schema).Write(actual, new BinaryEncoder(encoded));

        Assert.Equal(original.ToArray(), encoded.ToArray());
    }

    [Fact]
    public void Put_preserves_already_compatible_nested_collections()
    {
        var record = (ISpecificRecord)Activator.CreateInstance(typeof(NestedCollections))!;
        List<List<float>> value = [[1.25f, 2.5f]];

        record.Put(0, value);

        Assert.Same(value, record.Get(0));
    }

    [Fact]
    public void Put_reuses_compatible_inner_lists_when_the_outer_list_requires_conversion()
    {
        var record = (ISpecificRecord)Activator.CreateInstance(typeof(NestedCollections))!;
        List<float> populated = [1.25f, 2.5f];
        List<float> empty = [];
        List<IList<float>> source = [populated, empty];

        record.Put(0, source);

        var converted = Assert.IsType<List<List<float>>>(record.Get(0));
        Assert.Equal(2, converted.Count);
        Assert.Same(populated, converted[0]);
        Assert.Same(empty, converted[1]);
    }

    [Theory]
    [InlineData("ArrayBeforeBytes", false)]
    [InlineData("ArrayBeforeBytes", true)]
    [InlineData("BytesBeforeArray", false)]
    [InlineData("BytesBeforeArray", true)]
    public void Put_preserves_bytes_in_array_and_bytes_unions(string fieldName, bool empty)
    {
        // Apache's specific writer also matches byte[] as IList when an array branch comes first.
        // Exercise Put directly to isolate generated branch selection from that writer limitation.
        var record = (ISpecificRecord)Activator.CreateInstance(typeof(NestedCollections))!;
        var field = ((RecordSchema)record.Schema).Fields.Single(field => field.Name == fieldName);
        byte[] value = empty ? [] : [0, 1, 255];

        record.Put(field.Pos, value);

        Assert.Same(value, record.Get(field.Pos));
    }

    public static TheoryData<string, string, int> Cases()
    {
        var cases = new TheoryData<string, string, int>();
        string[] readers = ["generic-put", "specific", "datum"];
        string[] fields =
        [
            "Arrays", "ArrayMaps", "MapArrays", "Maps", "Deep",
            "NullableItems", "NullableValues", "Mixed", "TripleArrays", "QuadrupleArrays",
            "ArrayBeforeBytes", "BytesBeforeArray",
        ];

        foreach (var reader in readers)
        {
            foreach (var field in fields)
            {
                // Modes: 0 = null, 1 = empty, 2 = populated.
                for (var mode = 0; mode < 3; mode++)
                {
                    // ObjectCreator cannot resolve nested IDictionary names in Apache.Avro 1.12.2.
                    if (reader != "generic-put" && mode > 0 && field is "ArrayMaps" or "Maps" or "Deep")
                    {
                        continue;
                    }

                    cases.Add(reader, field, mode);
                }
            }

            cases.Add(reader, "Mixed", 3);
            cases.Add(reader, "Mixed", 4);
        }

        return cases;
    }

    private static object? Sample(string field, int mode)
    {
        if (mode == 0)
        {
            return null;
        }

        object[] values = mode == 1 ? [] : [1.25f, 2.5f];

        return field switch
        {
            "Arrays" or "Mixed" or "ArrayBeforeBytes" or "BytesBeforeArray" when mode < 3 => mode == 1
                ? Array.Empty<object>()
                : (object[])[values, Array.Empty<object>()],

            "TripleArrays" => IntegerArrays(3, mode),
            "QuadrupleArrays" => IntegerArrays(4, mode),

            "ArrayMaps" => mode == 1
                ? Array.Empty<object>()
                : (object[])
                [
                    new Dictionary<string, object> { ["v"] = 1.25f },
                    new Dictionary<string, object>(),
                ],

            "MapArrays" => mode == 1
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>
                {
                    ["v"] = values,
                    ["empty"] = Array.Empty<object>(),
                },

            "Maps" => mode == 1
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>
                {
                    ["v"] = new Dictionary<string, object> { ["x"] = 1.25f },
                    ["empty"] = new Dictionary<string, object>(),
                },

            "Deep" => mode == 1
                ? Array.Empty<object>()
                : (object[])
                [
                    new Dictionary<string, object>
                    {
                        ["v"] = (object[])[values, Array.Empty<object>()],
                    },
                ],

            "NullableItems" => mode == 1
                ? Array.Empty<object>()
                : (object?[])[null, values, Array.Empty<object>()],

            "NullableValues" => mode == 1
                ? new Dictionary<string, object>()
                : (object)new Dictionary<string, object?>
                {
                    ["null"] = null,
                    ["v"] = values,
                    ["empty"] = Array.Empty<object>(),
                },

            "Mixed" when mode == 3 => new Dictionary<string, object> { ["v"] = values },
            "Mixed" => "plain string",

            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
    }

    private static object[] IntegerArrays(int depth, int mode)
    {
        if (mode == 1)
        {
            return [];
        }

        if (depth == 1)
        {
            return [1, -2, int.MaxValue];
        }

        return [IntegerArrays(depth - 1, mode), Array.Empty<object>()];
    }
}
