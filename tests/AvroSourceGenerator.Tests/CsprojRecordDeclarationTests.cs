namespace AvroSourceGenerator.Tests;

public sealed class CsprojRecordDeclarationTests
{
    [Theory]
    [MemberData(nameof(RecordDeclarationSchemaPairs))]
    public Task Verify(string recordDeclaration, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        var config = new ProjectConfig { RecordDeclaration = recordDeclaration };

        return VerifySourceCode(schema, config);
    }

    public static MatrixTheoryData<string, string> RecordDeclarationSchemaPairs() => new(
        ["record", "class", "invalid"],
        ["record"]);
}
