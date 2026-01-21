namespace AvroSourceGenerator.Tests;

public sealed class CsprojAccessModifierTests
{
    [Theory]
    [MemberData(nameof(AccessModifierSchemaPairs))]
    public Task Verify(string accessModifier, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        var config = new ProjectConfig { AccessModifier = accessModifier };

        return VerifySourceCode(schema, config);
    }

    public static MatrixTheoryData<string, string> AccessModifierSchemaPairs() => new(
        ["public", "internal", "invalid"],
        ["enum", "error", "record", "protocol"]);
}
