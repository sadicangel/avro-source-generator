namespace AvroSourceGenerator.Tests.Apache;

public sealed class CsprojAccessModifierTests
{
    [Theory]
    [MemberData(nameof(AccessModifierSchemaPairs))]
    public Task Verify(string accessModifier, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        var config = new ProjectConfig { AccessModifier = accessModifier };

        return VerifySourceCode(schema, null, config);
    }

    public static MatrixTheoryData<string, string> AccessModifierSchemaPairs() => new MatrixTheoryData<string, string>(["public", "internal", "invalid"], ["enum", "error", "fixed", "record", "protocol"]);
}
