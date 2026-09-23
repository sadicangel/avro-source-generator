namespace AvroSourceGenerator.Tests.Apache.Snapshots;

public sealed class CsprojAccessModifierTests
{
    [Theory]
    [MemberData(nameof(AccessModifierSchemaPairs))]
    public Task Verify(string accessModifier, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        return Snapshot.Files([schemaType == "protocol" ? ProjectFile.Protocol(schema) : ProjectFile.Schema(schema)], config => config with { AccessModifier = accessModifier });
    }

    public static MatrixTheoryData<string, string> AccessModifierSchemaPairs() => new(["public", "internal", "invalid"], ["enum", "error", "fixed", "record", "protocol"]);
}
