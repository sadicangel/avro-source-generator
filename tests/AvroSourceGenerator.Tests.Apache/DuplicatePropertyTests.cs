using System.Text.Json;

namespace AvroSourceGenerator.Tests.Apache;

public sealed class DuplicatePropertyTests
{
    [Fact]
    public void Apache_avro_keeps_the_last_custom_property_value()
    {
        const string json = """{"type":"record","name":"R","fields":[],"x":1,"x":2}""";

        using var serialized = JsonDocument.Parse(Avro.Schema.Parse(json).ToString());

        Assert.Equal(2, serialized.RootElement.GetProperty("x").GetInt32());
    }
}
