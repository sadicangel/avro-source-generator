namespace AvroSourceGenerator.Tests;

public sealed class SchemaReferenceTests
{
    [Fact]
    public Task Verify() => VerifySourceCode("""
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
                "type": "This.Is.A.Full.Name",
                "doc": "This is a reference to the record 'Name' by its full name 'This.Is.A.Full.Name'."
            },
            {
                "name": "Field3",
                "type": {
                    "type": "record",
                    "name": "OtherName",
                    "namespace": "This.Is.A.Full",
                    "fields": [
                        {
                            "name": "Field4",
                            "type": "Name",
                            "doc": "This is a reference to the record 'Name' defined in the same namespace as the enclosing type 'OtherName'."
                        }
                    ]
                }
            }
        ]
    }
    """);

    [Fact]
    public Task Diagnostic() => VerifyDiagnostic($$"""
    {
        "type": "record",
        "name": "MissingReference",
        "doc": "This record contains a field that references a type that is not defined in the schema.",
        "fields": [
            {
                "name": "MissingField",
                "type": "This.Type.Was.Not.Defined.In.The.Schema"
            }
        ]
    }
    """);
}
