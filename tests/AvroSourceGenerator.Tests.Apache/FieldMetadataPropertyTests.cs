namespace AvroSourceGenerator.Tests.Apache;

public class FieldMetadataPropertyTests
{
    [Theory]
    [InlineData("record"), InlineData("error")]
    public Task Verify(string @class)
    {
        return VerifySourceCode(
            $$"""
            {
                "type": "{{@class}}",
                "name": "Class",
                "namespace": "SchemaNamespace",
                "fields": [
                    {
                        "type": ["null", "string"],
                        "name": "Field",
                        "tags": ["Tag1", "Tag2"],
                        "parent": {
                            "name": "object1"
                        }
                    }
                ]
            }
            """);
    }
}
