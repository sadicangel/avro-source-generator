using System.Reflection;

namespace AvroSourceGenerator.IntegrationTests.Apache;

public sealed class AvroSchemaTests
{
    public static TheoryData<FileInfo> GetSchemaFileNames() =>
        [.. new DirectoryInfo("Schemas").GetFiles().ExceptBy(["UserDirectory.avsc"], x => x.Name)];

    [Theory]
    [MemberData(nameof(GetSchemaFileNames))]
    public void Generated_schemas_are_equal_to_schemas_parsed_by_apache_avro(FileInfo avsc)
    {
        using var stream = avsc.OpenRead();
        using var reader = new StreamReader(stream);

        var expectedSchema = Avro.Schema.Parse(reader.ReadToEnd());
        var actualSchema = GetGeneratedTypeSchema(Path.ChangeExtension(avsc.Name, null));

        Assert.Equal(expectedSchema, actualSchema);
    }

    private static Avro.Schema GetGeneratedTypeSchema(string typeName)
    {
        var type = Type.GetType($"AvroSourceGenerator.IntegrationTests.Schemas.{typeName}", throwOnError: true)!;
        var field = type.GetField("s_schema", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        return (Avro.Schema)field.GetValue(null)!;
    }
}
