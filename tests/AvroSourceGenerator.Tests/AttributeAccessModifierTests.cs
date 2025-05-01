using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class AttributeAccessModifierTests
{
    [Theory]
    [MemberData(nameof(AccessModifierSchemaPairs))]
    public Task Verify(string accessModifier, string schemaType)
    {
        var schema = s_schemas[schemaType];

        var source = s_sources[schemaType].Replace("$accessModifier$", accessModifier);

        return TestHelper.VerifySourceCode(schema, source);
    }

    public static MatrixTheoryData<string, string> AccessModifierSchemaPairs() => new(
        ["", "public", "internal", "file"],
        ["error", "fixed", "record"]);

    private static readonly Dictionary<string, string> s_schemas = new()
    {
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

    private static readonly Dictionary<string, string> s_sources = new()
    {
        ["error"] = """
        using System;
        using AvroSourceGenerator;

        namespace SchemaNamespace;
        
        [Avro]
        $accessModifier$ partial class Error;
        """,

        ["fixed"] = """
        using System;
        using AvroSourceGenerator;

        namespace SchemaNamespace;

        [Avro]
        $accessModifier$ partial class Fixed;
        """,

        ["record"] = """
        using System;
        using AvroSourceGenerator;

        namespace SchemaNamespace;

        [Avro]
        $accessModifier$ partial class Record;
        """
    };
}
