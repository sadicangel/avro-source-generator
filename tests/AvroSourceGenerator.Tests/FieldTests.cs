namespace AvroSourceGenerator.Tests;

public class FieldTests
{
    public static MatrixTheoryData<string, string> Primitives = new(
        ["record", "error"],
        ["null", "boolean", "int", "long", "float", "double", "bytes", "string"]);

    [Theory]
    [MemberData(nameof(Primitives))]
    public Task Verify_Primitive(string @class, string field)
    {
        var schema = $$"""
        {
            "type": "{{@class}}",
            "name": "Class",
            "fields": [
                {
                    "type": "{{field}}",
                    "name": "Field"
                }
            ]
        }
        """;
        return TestHelper.VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(Primitives))]
    public Task Verify_Primitive_Nullable(string @class, string field)
    {
        var schema = $$"""
        {
            "type": "{{@class}}",
            "name": "Class",
            "fields": [
                {
                    "type": ["null", "{{field}}"],
                    "name": "Field"
                }
            ]
        }
        """;
        return TestHelper.VerifySourceCode(schema);
    }
}
