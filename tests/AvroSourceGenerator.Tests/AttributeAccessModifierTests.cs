using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class AttributeAccessModifierTests
{
    [Theory]
    [MemberData(nameof(AccessModifierSchemaPairs))]
    public Task Verify(string accessModifier, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        var source = s_sources[schemaType].Replace("$accessModifier$", accessModifier);

        return TestHelper.VerifySourceCode(schema, source);
    }

    public static MatrixTheoryData<string, string> AccessModifierSchemaPairs() => new(
        ["", "public", "internal", "file"],
        ["error", "fixed", "record", "protocol"]);

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
        """,

        ["protocol"] = """
        using System;
        using AvroSourceGenerator;

        namespace SchemaNamespace;

        [Avro]
        $accessModifier$ partial class RpcProtocol;
        """,
    };
}
