namespace AvroSourceGenerator.Tests.Apache;

public class FieldEnumTests
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
                        "type": {
                            "type": "enum",
                            "name": "Enum",
                            "symbols": ["A", "B", "C"]
                        },
                        "name": "Field"
                    },
                    {
                        "type": ["null", "Enum"],
                        "name": "NullableField"
                    }
                ]
            }
            """);
    }
}
