namespace AvroSourceGenerator.Tests;

public class FieldCollectionTests
{
    public static MatrixTheoryData<string, string> Primitives => new(["record", "error"], ["array<string>", "map<string>"]);

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
                        "type": {{TestSchemas.Get(field)}},
                        "name": "Field"
                    },
                    {
                        "type": ["null", {{TestSchemas.Get(field)}}],
                        "name": "NullableField"
                    }
                ]
            }
            """;
        return VerifySourceCode(schema);
    }
}
