using System.Reflection;

namespace AvroSourceGenerator.IntegrationTests;

public sealed class AvroProtocolTests
{
    public static TheoryData<FileInfo> GetProtocolFileNames() =>
        [.. new DirectoryInfo("Schemas").GetFiles().ExceptBy(AvroSchemaTests.GetSchemaFileNames().Select(x => x.Data.Name), x => x.Name)];

    [Theory]
    [MemberData(nameof(GetProtocolFileNames))]
    public void Generated_protocols_are_equal_to_protocols_parsed_by_apache_avro(FileInfo avsc)
    {
        using var stream = avsc.OpenRead();
        using var reader = new StreamReader(stream);

        var expectedProtocol = Avro.Protocol.Parse(reader.ReadToEnd());
        var actualProtocol = GetGeneratedTypeProtocol(Path.ChangeExtension(avsc.Name, null));

        Assert.Equal(expectedProtocol, actualProtocol);
    }

    private static Avro.Protocol GetGeneratedTypeProtocol(string typeName)
    {
        var type = Type.GetType($"AvroSourceGenerator.IntegrationTests.Schemas.{typeName}", throwOnError: true)!;
        var field = type.GetField("s_protocol", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        return (Avro.Protocol)field.GetValue(null)!;
    }
}
