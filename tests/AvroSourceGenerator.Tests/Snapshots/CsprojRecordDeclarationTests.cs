namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class CsprojRecordDeclarationTests
{
    [Theory]
    [MemberData(nameof(RecordDeclarationSchemaPairs))]
    public Task Verify(string recordDeclaration, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        return Snapshot.Schema(schema, config => config with { RecordDeclaration = recordDeclaration });
    }

    public static MatrixTheoryData<string, string> RecordDeclarationSchemaPairs() => new MatrixTheoryData<string, string>(["record", "class", "invalid"], ["record", "error"]);
}
