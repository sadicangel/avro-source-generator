using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaOutputTests
{
    [Fact]
    public Task Verify_Array_Schema_Output() => TestHelper.VerifySourceCode("""
    {
        "type": "record",
        "namespace": "SchemaNamespace",
        "name": "Container",
        "fields": [
            {
                "name": "ArrayField",
                "type": {
                    "type": "array",
                    "items": {
                        "type": "record",
                        "name": "Item",
                        "fields": [
                            {
                                "name": "Field1",
                                "type": ["null", "string"]
                            }
                        ]
                    }
                }
            }
        ]
    }
    """);

    [Fact]
    public Task Verify_Map_Schema_Output() => TestHelper.VerifySourceCode("""
    {
        "type": "record",
        "namespace": "SchemaNamespace",
        "name": "Container",
        "fields": [
            {
                "name": "MapField",
                "type": {
                    "type": "map",
                    "values": {
                        "type": "record",
                        "name": "Item",
                        "fields": [
                            {
                                "name": "Field1",
                                "type": ["null", "string"]
                            }
                        ]
                    }
                }
            }
        ]
    }
    """);
}
