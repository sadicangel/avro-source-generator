using AvroSourceGenerator.IntegrationTests.Schemas;

namespace AvroSourceGenerator.IntegrationTests;

public sealed class BytesDefaultSerializationTests
{
    [Fact]
    public void Byte_fields_with_schema_defaults_survive_binary_serialization()
    {
        var expected = new BytesDefaults
        {
            Plain = "abc"u8.ToArray(),
            Base64Like = "ABCD"u8.ToArray(),
            PreviousFixture = "NDI="u8.ToArray(),
            Empty = [],
            Escaped = [0, 9, 10, 13, 34, 92, 127, 128, 255, 233],
        };
        var actual = BinarySerialization.Roundtrip(expected);

        Assert.Equal("abc"u8, actual.Plain);
        Assert.Equal("ABCD"u8, actual.Base64Like);
        Assert.Equal("NDI="u8, actual.PreviousFixture);
        Assert.Empty(actual.Empty);
        Assert.Equal(new byte[] { 0, 9, 10, 13, 34, 92, 127, 128, 255, 233 }, actual.Escaped);
    }
}
