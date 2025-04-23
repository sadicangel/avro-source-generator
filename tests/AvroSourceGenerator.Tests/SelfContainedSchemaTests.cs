using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class SelfContainedSchemaTests
{
    [Fact]
    public Task Verify_record_schema_is_self_contained() => TestHelper.VerifySourceCode("""
    [
        {
            "type": "record",
            "name": "Record1",
            "namespace": "Namespace1",
            "fields": [
                {
                    "name": "field1",
                    "type": "string"
                }
            ]
        },
        {
            "type": "record",
            "name": "Record2",
            "namespace": "Namespace1",
            "doc": "This class must contain the whole schema, including the record1 schema.",
            "fields": [
                {
                    "name": "field1",
                    "type": "Record1"
                }
            ]
        }
    ]
    """);
}
