using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.UnitTests;

public sealed class ApacheFunctionsTests
{
    [Theory]
    [InlineData("array")]
    [InlineData("map")]
    public void Collection_items_with_native_unions_need_a_converter_even_for_scalar_branches(string collectionType)
    {
        var properties = ImmutableSortedDictionary<string, JsonElement>.Empty;
        ImmutableArray<AvroSchema> members = [AvroSchema.String, AvroSchema.Float];
        var generatedUnion = new UnionTypeSchema(new SchemaName("ExampleUnion"), new CSharpName("ExampleUnion"), members);
        var union = UnionSchema.Create(members, useNullableReferenceTypes: true) with
        {
            CSharpName = generatedUnion.CSharpName,
            UnderlyingSchema = generatedUnion,
        };
        AvroSchema collection = collectionType == "array"
            ? new ArraySchema(union, null, properties)
            : new MapSchema(union, null, properties);
        var schema = new RecordSchema(new SchemaName("ConvertersRecord"), null, [],
            [new Field("Value", collection, collection, null, [], null, null, properties, null)], properties);

        Assert.Equal([collectionType], RequiredCollectionConverters(schema));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void Required_collection_converters_inspect_record_and_error_fields(bool error, bool nested, bool mapFirst)
    {
        var properties = ImmutableSortedDictionary<string, JsonElement>.Empty;
        var array = new ArraySchema(AvroSchema.Float, null, properties);
        var map = new MapSchema(AvroSchema.Float, null, properties);

        ImmutableArray<Field> fields = nested
            ? [CreateField(mapFirst
                    ? new MapSchema(array, null, properties)
                    : new ArraySchema(map, null, properties))]
            : [CreateField(mapFirst ? map : array), CreateField(mapFirst ? array : map)];
        AvroSchema schema = error
            ? new ErrorSchema(new SchemaName("ConvertersError"), null, [], fields, properties)
            : new RecordSchema(new SchemaName("ConvertersRecord"), null, [], fields, properties);

        Assert.Equal(nested ? ["array", "map"] : [], RequiredCollectionConverters(schema));

        Field CreateField(AvroSchema schema) => new(
            "Value", schema, schema, null, [], null, null, properties, null);
    }

    private static string[] RequiredCollectionConverters(AvroSchema schema)
    {
        var functionsType = typeof(AvroTemplate).Assembly.GetType("AvroSourceGenerator.Templating.ApacheFunctions", throwOnError: true)!;
        var method = functionsType.GetMethod("RequiredCollectionConverters");
        Assert.NotNull(method);
        var requiredConverters = (Func<AvroSchema, string[]>)method.CreateDelegate(typeof(Func<AvroSchema, string[]>));
        return requiredConverters(schema);
    }
}
