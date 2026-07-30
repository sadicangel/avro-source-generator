namespace AvroSourceGenerator.Tests.Apache.Bugs;

public sealed class FieldOrderCodeGenCompatibilityTests
{
    [Fact]
    public void Apache_CodeGen_drops_field_order_from_generated_schema()
    {
        const string SchemaJson =
            """
            {
              "type": "record",
              "name": "Person",
              "namespace": "Example",
              "fields": [
                {
                  "name": "name",
                  "type": "string",
                  "order": "descending"
                }
              ]
            }
            """;

        var codeGen = new Avro.CodeGen();
        codeGen.AddSchema(SchemaJson);

        codeGen.GenerateCode();
        var generatedCode = codeGen.GetTypes().Single().Value;

        Assert.Contains("\\\"name\\\":\\\"name\\\"", generatedCode);
        Assert.DoesNotContain("\\\"order\\\":\\\"descending\\\"", generatedCode);
    }
}
