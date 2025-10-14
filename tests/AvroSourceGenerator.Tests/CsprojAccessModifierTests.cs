using AvroSourceGenerator.Tests.Helpers;
using AvroSourceGenerator.Tests.Setup;

namespace AvroSourceGenerator.Tests;

public sealed class CsprojAccessModifierTests
{
    [Theory]
    [MemberData(nameof(AccessModifierSchemaPairs))]
    public Task Verify(string accessModifier, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        var config = new ProjectConfig() with { AccessModifier = accessModifier };

        return TestHelper.VerifySourceCode(schema, default, config);
    }

    public static MatrixTheoryData<string, string> AccessModifierSchemaPairs() => new(
        ["public", "internal", "invalid"],
        ["enum", "error", "fixed", "record", "protocol"]);
}
