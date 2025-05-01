using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;
public sealed class CsprojAccessModifierTests
{
    [Theory]
    [MemberData(nameof(AccessModifierSchemaPairs))]
    public Task Verify(string accessModifier, string schemaType)
    {
        var schema = s_schemas[schemaType];

        var config = ProjectConfig.Default with
        {
            GlobalOptions = new Dictionary<string, string>
            {
                ["AvroSourceGeneratorAccessModifier"] = accessModifier
            }
        };

        return TestHelper.VerifySourceCode(schema, default, config);
    }

    public static MatrixTheoryData<string, string> AccessModifierSchemaPairs() => new(
        ["public", "internal", "invalid"],
        ["enum", "error", "fixed", "record"]);

    private static readonly Dictionary<string, string> s_schemas = new()
    {
        ["enum"] = """
        {
            "type": "enum",
            "name": "Enum",
            "namespace": "SchemaNamespace",
            "symbols": []
        }
        """,

        ["error"] = """
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "fields": []
        }
        """,

        ["fixed"] = """
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "size": 16
        }
        """,

        ["record"] = """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": []
        }
        """
    };
}
