using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class SchemaReferenceTests
{
    [Fact]
    public Task Verify() => TestHelper.VerifySourceCode("""
    {
        "type": "record",
        "name": "Wrapper",
        "fields": [
            {
                "name": "Field1",
                "type": {
                    "type": "record",
                    "name": "Name",
                    "namespace": "This.Is.A.Full",
                    "fields": []
                }
            },
            {
                "name": "Field2",
                "type": "This.Is.A.Full.Name"
            }
        ]
    }
    """);
}
