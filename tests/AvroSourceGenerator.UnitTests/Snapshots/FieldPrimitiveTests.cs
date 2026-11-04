namespace AvroSourceGenerator.UnitTests.Snapshots;

public class FieldPrimitiveTests
{
    public static MatrixTheoryData<string, string> Primitives => new MatrixTheoryData<string, string>(["record", "error"], ["null", "boolean", "int", "long", "float", "double", "bytes", "string"]);

    [Theory]
    [MemberData(nameof(Primitives))]
    public Task Verify(string @class, string field)
    {
        // A null-only union has one branch; ["null", "null"] is invalid Avro.
        var nullableType = field is "null" ? """["null"]""" : $$"""["null", "{{field}}"]""";
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
                        "type": {{nullableType}},
                        "name": "NullableField"
                    }
                ]
            }
            """;
        return Snapshot.Schema(schema);
    }
}
