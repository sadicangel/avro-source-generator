namespace AvroSourceGenerator.Tests;

public class FieldPrimitiveTests
{
    public static MatrixTheoryData<string, string> Primitives => new(
        ["record", "error"],
        ["null", "boolean", "int", "long", "float", "double", "bytes", "string"]);

    [Theory]
    [MemberData(nameof(Primitives))]
    public Task Verify(string @class, string field)
    {
        var schema = $$"""
        {
            "type": "{{@class}}",
            "name": "Class",
            "namespace": "SchemaNamespace",
            "fields": [
                {
                    "type": "{{field}}",
                    "name": "Field"
                },
                {
                    "type": ["null", "{{field}}"],
                    "name": "NullableField"
                }
            ]
        }
        """;
        return VerifySourceCode(schema);
    }
}
