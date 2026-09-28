using System.Text.Json;
using Avro;

namespace AvroSourceGenerator.Tests.Apache;

public sealed class DuplicatePropertyTests
{
    [Fact]
    public void Apache_avro_keeps_the_last_custom_property_value()
    {
        const string Json = """{"type":"record","name":"R","fields":[],"x":1,"x":2}""";

        using var serialized = JsonDocument.Parse(Schema.Parse(Json).ToString());

        Assert.Equal(2, serialized.RootElement.GetProperty("x").GetInt32());
    }
}
