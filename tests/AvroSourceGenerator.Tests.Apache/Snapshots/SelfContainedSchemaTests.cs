namespace AvroSourceGenerator.Tests.Apache.Snapshots;

public sealed class SelfContainedSchemaTests
{
    [Fact]
    public Task Verify() => Snapshot.Schema(
        """
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
