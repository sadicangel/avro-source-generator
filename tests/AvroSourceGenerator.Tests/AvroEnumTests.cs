namespace AvroSourceGenerator.Tests;

public class AvroEnumTests
{
    [Theory]
    [InlineData("EnumName"), InlineData("enum_name"), InlineData("public"), InlineData("enum")]
    public Task VerifyName(string name) => TestHelper.VerifySourceCode($$"""
    {
        "type": "enum",
        "name": "{{name}}",
        "namespace": "SchemaNamespace",
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("[]")]
    public Task VerifyNameDiagnostic(string name)
    {
        var avro = $$"""
        {
            "type": "enum",
            "name": {{name}},
            "namespace": "SchemaNamespace",
            "symbols": []
        }
        """;

        return TestHelper.VerifyDiagnostic(avro);
    }

    [Theory]
    [InlineData("null"), InlineData("\"EnumSchemaNamespace\"")]
    [InlineData("\"enum\""), InlineData("\"name1.enum.name2\"")]
    [InlineData("\"\""), InlineData("[]")]
    public Task Verify_Namespace(string @namespace)
    {
        var avro = $$"""
        {
            "type": "enum",
            "name": "TestEnum",
            "namespace": {{@namespace}},
            "symbols": []
        }
        """;

        return TestHelper.VerifySourceCode(avro);
    }

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    [InlineData("1"), InlineData("[]"), InlineData("{}")]
    public Task Verify_Documentation(string doc)
    {
        var avro = $$"""
        {
            "type": "enum",
            "name": "TestEnum",
            "namespace": "SchemaNamespace",
            "doc": {{doc}},
            "symbols": []
        }
        """;

        return TestHelper.VerifySourceCode(avro);
    }

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\"]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    [InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Aliases(string aliases)
    {
        var avro = $$"""
        {
            "type": "enum",
            "name": "TestEnum",
            "namespace": "SchemaNamespace",
            "aliases": {{aliases}},
            "symbols": []
        }
        """;

        return TestHelper.VerifySourceCode(avro);
    }

    [Theory]
    [InlineData("[]"), InlineData("[\"A\"]"), InlineData("[\"B\", \"C\"]")]
    [InlineData("null"), InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Symbols(string symbols)
    {
        var avro = $$"""
        {
            "type": "enum",
            "name": "TestEnum",
            "namespace": "SchemaNamespace",
            "symbols": {{symbols}}
        }
        """;

        return TestHelper.VerifySourceCode(avro);
    }
}
