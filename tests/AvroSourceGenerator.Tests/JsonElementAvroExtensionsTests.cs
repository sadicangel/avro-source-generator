namespace AvroSourceGenerator.Tests;

public sealed class JsonElementAvroExtensionsTests
{
    // TODO: Uncomment these tests once we find a way to access the internal JsonElementAvroExtensions class in the test project. We may need to make it public or use InternalsVisibleTo.
    //[Fact]
    //public void GetRequiredSchemaName_ThrowsWhenOptionalCanonicalPathReturnsDefault()
    //{
    //    var schema = Parse(
    //        """
    //        {
    //          "type": "record",
    //          "fields": []
    //        }
    //        """);

    //    var exception = Assert.Throws<InvalidSchemaException>(() => schema.GetRequiredSchemaName(null));

    //    Assert.Contains("'name' property is required in schema:", exception.Message);
    //}

    //[Fact]
    //public void GetRequiredSchemaName_ReturnsResolvedSchemaName()
    //{
    //    var schema = Parse(
    //        """
    //        {
    //          "name": "SimpleName"
    //        }
    //        """);

    //    var result = schema.GetRequiredSchemaName("Containing.Namespace");

    //    Assert.Equal(new SchemaName("SimpleName", "Containing.Namespace"), result);
    //}

    //private static JsonElement Parse(string json)
    //{
    //    using var document = JsonDocument.Parse(json);
    //    return document.RootElement.Clone();
    //}
}
