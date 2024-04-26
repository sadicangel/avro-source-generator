using AutoFixture;
using AvroNet.Example;

static ExampleFixed MakeExampleFixed()
{
    var random = new Random();
    var @fixed = new ExampleFixed();
    random.NextBytes(@fixed.Value);
    return @fixed;
}

var fixture = new Fixture()
    .Build<Test>()
    .With(f => f.fixed_field, MakeExampleFixed)
    .With(f => f.null_fixed_field, MakeExampleFixed);

var test = fixture.Create<Test>();

Console.WriteLine(test);

//internal readonly record struct AvroSchema(JsonElement Json)
//{
//    public JsonElement Name { get => Json.GetProperty("name"); }
//    public JsonElement Namespace { get => Json.GetProperty("namespace"); }
//    public JsonElement Type { get => Json.GetProperty("type"); }
//}

//internal static class AvroSchemaJsonDocumentExtensions
//{
//    public static IEnumerable<AvroSchema> EnumerateSchemas(this JsonDocument document)
//    {
//        var rootSchema = new AvroSchema(document.RootElement);
//        yield return rootSchema;
//        foreach (var schema in rootSchema.EnumerateSchemas())
//            yield return schema;
//    }
//}