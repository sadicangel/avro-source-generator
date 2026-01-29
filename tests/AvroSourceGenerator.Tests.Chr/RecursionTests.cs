namespace AvroSourceGenerator.Tests.Chr;

public class RecursionTests
{
    [Fact]
    public Task Verify_Record()
    {
        return VerifySourceCode(
            """
            {
              "type": "record",
              "name": "A",
              "namespace": "ex",
              "fields": [
                {
                  "name": "child",
                  "type": {
                    "type": "record",
                    "name": "B",
                    "fields": [
                      {
                        "name": "owner",
                        "type": "A"
                      }
                    ]
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public Task Verify_Array()
    {
        return VerifySourceCode(
            """
            {
              "type": "record",
              "name": "A",
              "namespace": "ex",
              "fields": [
                {
                  "name": "items",
                  "type": {
                    "type": "array",
                    "items": "A"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public Task Verify_Map()
    {
        return VerifySourceCode(
            """
            {
              "type": "record",
              "name": "A",
              "namespace": "ex",
              "fields": [
                {
                  "name": "lookup",
                  "type": {
                    "type": "map",
                    "values": "A"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public Task Verify_Union()
    {
        return VerifySourceCode(
            """
            {
              "type": "record",
              "name": "Node",
              "namespace": "ex",
              "fields": [
                { "name": "value", "type": "int" },
                { "name": "next", "type": ["null", "Node"], "default": null }
              ]
            }
            """);
    }
}
