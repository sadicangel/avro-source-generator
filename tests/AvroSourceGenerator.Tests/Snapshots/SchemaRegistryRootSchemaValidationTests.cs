namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class SchemaRegistryRootSchemaValidationTests
{
    [Theory]
    [MemberData(nameof(InvalidRootSchemas))]
    public Task Diagnostic(string schemaType) =>
        VerifyDiagnostic(GetInvalidRootSchema(schemaType));

    [Theory]
    [MemberData(nameof(ValidRootSchemas))]
    public Task Verify(string schemaType) =>
        VerifySourceCode(GetValidRootSchema(schemaType));

    public static TheoryData<string> InvalidRootSchemas() => new("string", "array<string>", "map<string>");

    public static TheoryData<string> ValidRootSchemas() => new("array<record>", "map<record>", "[null, record]");

    private static string GetInvalidRootSchema(string schemaType) => schemaType switch
    {
        "string" => "\"string\"",
        "array<string>" =>
            """
            {
              "type": "array",
              "items": "string"
            }
            """,
        "map<string>" =>
            """
            {
              "type": "map",
              "values": "string"
            }
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(schemaType), schemaType, null),
    };

    private static string GetValidRootSchema(string schemaType) => schemaType switch
    {
        "array<record>" =>
            """
            {
              "type": "array",
              "items": {
                "type": "record",
                "namespace": "Demo",
                "name": "ArrayRecord",
                "fields": []
              }
            }
            """,
        "map<record>" =>
            """
            {
              "type": "map",
              "values": {
                "type": "record",
                "namespace": "Demo",
                "name": "MapRecord",
                "fields": []
              }
            }
            """,
        "[null, record]" =>
            """
            [
              "null",
              {
                "type": "record",
                "namespace": "Demo",
                "name": "UnionRecord",
                "fields": []
              }
            ]
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(schemaType), schemaType, null),
    };

    private const string NamedRecord = """
        {
          "type": "record",
          "name": "OrderCreated",
          "namespace": "Demo",
          "fields": []
        }
        """;
}
