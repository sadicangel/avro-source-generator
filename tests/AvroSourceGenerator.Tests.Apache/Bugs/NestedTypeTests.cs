namespace AvroSourceGenerator.Tests.Apache.Bugs;

public class NestedTypeTests
{
    [Fact]
    public Task Verify() => VerifySourceCode(
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
