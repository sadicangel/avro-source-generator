using System.Reflection;

namespace AvroSourceGenerator.IntegrationTests.Apache;

public sealed class AvroProtocolTests
{
    public static TheoryData<FileInfo> GetProtocolFileNames() =>
        [.. new DirectoryInfo("Schemas").GetFiles("*.avpr")];

    [Theory]
    [MemberData(nameof(GetProtocolFileNames))]
    public void Generated_protocols_are_equal_to_protocols_parsed_by_apache_avro(FileInfo avpr)
    {
        using var stream = avpr.OpenRead();
        using var reader = new StreamReader(stream);

        var expectedProtocol = Avro.Protocol.Parse(reader.ReadToEnd());
        var actualProtocol = GetGeneratedTypeProtocol(Path.ChangeExtension(avpr.Name, null));

        Assert.Equal(expectedProtocol, actualProtocol);
    }

    private static Avro.Protocol GetGeneratedTypeProtocol(string typeName)
    {
        var type = Type.GetType($"AvroSourceGenerator.IntegrationTests.Schemas.{typeName}", throwOnError: true)!;
        var field = type.GetField("protocol", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        return (Avro.Protocol)field.GetValue(null)!;
    }
}
