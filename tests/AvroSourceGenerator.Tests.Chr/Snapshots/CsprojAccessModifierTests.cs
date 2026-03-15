namespace AvroSourceGenerator.Tests.Chr.Snapshots;

public sealed class CsprojAccessModifierTests
{
    [Theory]
    [MemberData(nameof(AccessModifierSchemaPairs))]
    public Task Verify(string accessModifier, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        return Snapshot.Schema(schema, config => config with { AccessModifier = accessModifier });
    }

    public static MatrixTheoryData<string, string> AccessModifierSchemaPairs() => new MatrixTheoryData<string, string>(["public", "internal", "invalid"], ["enum", "error", "record", "protocol"]);
}
