using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.UnitTests;

public sealed class ApacheFunctionsTests
{
    [Theory]
    [InlineData("array", true)]
    [InlineData("map", true)]
    [InlineData("scalar", false)]
    public void Typed_unions_need_collection_converters_only_for_collection_members(string memberType, bool expected)
    {
        var properties = ImmutableSortedDictionary<string, JsonElement>.Empty;
        AvroSchema member = memberType switch
        {
            "array" => new ArraySchema(AvroSchema.Float, null, properties),
            "map" => new MapSchema(AvroSchema.Float, null, properties),
            _ => AvroSchema.Float,
        };
        ImmutableArray<AvroSchema> members = [AvroSchema.String, member];
        var generatedUnion = new UnionTypeSchema(new SchemaName("ExampleUnion"), new CSharpName("ExampleUnion"), members);
        var union = UnionSchema.Create(members, useNullableReferenceTypes: true) with
        {
            CSharpName = generatedUnion.CSharpName,
            UnderlyingSchema = generatedUnion,
        };
        var functionsType = typeof(AvroTemplate).Assembly.GetType("AvroSourceGenerator.Templating.ApacheFunctions", throwOnError: true)!;
        var method = functionsType.GetMethod("RequiresCollectionConversion");
        Assert.NotNull(method);
        var requiresCollectionConversion = (Func<AvroSchema, bool>)method.CreateDelegate(typeof(Func<AvroSchema, bool>));

        Assert.Equal(expected, requiresCollectionConversion(union));
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

        var functionsType = typeof(AvroTemplate).Assembly.GetType("AvroSourceGenerator.Templating.ApacheFunctions", throwOnError: true)!;
        var method = functionsType.GetMethod("RequiredCollectionConverters");
        Assert.NotNull(method);
        var requiredConverters = (Func<AvroSchema, string[]>)method.CreateDelegate(typeof(Func<AvroSchema, string[]>));

        Assert.Equal(nested ? ["array", "map"] : [], requiredConverters(schema));

        Field CreateField(AvroSchema schema) => new(
            "Value", schema, schema, null, [], null, null, null, properties, null);
    }
}
