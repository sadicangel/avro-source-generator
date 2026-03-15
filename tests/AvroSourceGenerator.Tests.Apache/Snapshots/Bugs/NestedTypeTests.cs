namespace AvroSourceGenerator.Tests.Apache.Snapshots.Bugs;

public class NestedTypeTests
{
    [Fact]
    public Task Verify() => Snapshot.Schema(
        """
        {
          "fields": [
            {
              "description": "The displayName of the customer",
              "name": "displayName",
              "type": {
                "avro.java.string": "String",
                "type": "string"
              }
            }
          ],
          "name": "SomeSchema",
          "type": "record"
        }
        """);
}
