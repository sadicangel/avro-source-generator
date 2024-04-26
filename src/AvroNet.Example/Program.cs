using AutoFixture;
using AvroNet.Example;

var test = new Fixture().Create<Test>();

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