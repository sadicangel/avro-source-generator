namespace AvroSourceGenerator.Tests;

public class FieldDefaultTests
{
    public static TheoryData<string, string> Defaults => new([
        ("null", "null"),
        ("boolean", "true"),
        ("int", "42"),
        ("long", "42"),
        ("float", "42.0"),
        ("double", "42.0"),
        ("bytes", @"""NDI="""),
        ("string", @"""FortyTwo"""),
        ("enum<A,B,C>", @"""B""")]);

    [Theory]
    [MemberData(nameof(Defaults))]
    public Task Verify(string schemaType, string defaultValue)
    {
        var schema = TestSchemas.Get("record").With("fields", new JsonArray(new JsonObject(new Dictionary<string, JsonNode?>
        {
            ["name"] = "Field",
            ["type"] = TestSchemas.Get(schemaType),
            ["default"] = JsonNode.Parse(defaultValue),
        }))).ToString();

        return VerifySourceCode(schema);
    }
}
