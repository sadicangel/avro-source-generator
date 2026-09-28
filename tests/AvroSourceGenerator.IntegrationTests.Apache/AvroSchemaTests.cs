using System.Reflection;
using System.Text.Json.Nodes;
using Avro;

namespace AvroSourceGenerator.IntegrationTests.Apache;

public sealed class AvroSchemaTests
{
    public static TheoryData<FileInfo> GetSchemaFileNames() =>
        [.. new DirectoryInfo("Schemas").GetFiles("*.avsc")];

    [Theory]
    [MemberData(nameof(GetSchemaFileNames))]
    public void Generated_schemas_are_equal_to_schemas_parsed_by_apache_avro(FileInfo avsc)
    {
        using var stream = avsc.OpenRead();
        using var reader = new StreamReader(stream);

        var source = reader.ReadToEnd();
        if (avsc.Name == "LogicalTypes.avsc")
        {
            // Apache.Avro's fixed-backed duration serialization is rejected by Schema Registry.
            var json = JsonNode.Parse(source)!;
            json["fields"]![2]!["type"]!.AsObject().Remove("logicalType");
            source = json.ToJsonString();
        }

        var expectedSchema = Schema.Parse(source);
        var actualSchema = GetGeneratedTypeSchema(Path.ChangeExtension(avsc.Name, null));

        Assert.Equal(expectedSchema, actualSchema);
    }

    private static Schema GetGeneratedTypeSchema(string typeName)
    {
        var type = Type.GetType($"AvroSourceGenerator.IntegrationTests.Schemas.{typeName}", throwOnError: true)!;
        var field = type.GetField("_SCHEMA", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        return (Schema)field.GetValue(null)!;
    }
}
