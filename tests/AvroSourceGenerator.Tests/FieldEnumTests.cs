using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public class FieldEnumTests
{
    [Theory]
    [InlineData("record"), InlineData("error")]
    public Task Verify(string @class)
    {
        var schema = $$"""
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
        """;
        return TestHelper.VerifySourceCode(schema);
    }
}
