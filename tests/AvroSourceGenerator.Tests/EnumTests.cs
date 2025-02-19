namespace AvroSourceGenerator.Tests;

public class EnumTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("invalid")]
    public Task Verify_AccessModifier_Global(string accessModifier)
    {
        var schema = """
        {
            "type": "enum",
            "name": "Enum",
            "namespace": "SchemaNamespace",
            "symbols": []
        }
        """;

        var config = ProjectConfig.Default with
        {
            GlobalOptions = new Dictionary<string, string>
            {
                ["AvroSourceGeneratorAccessModifier"] = accessModifier
            }
        };

        return TestHelper.VerifySourceCode(schema, default, config);
    }

    [Theory]
    [InlineData("EnumName"), InlineData("enum_name"), InlineData("enum")]
    public Task Verify_Name(string name) => TestHelper.VerifySourceCode($$"""
    {
        "type": "enum",
        "name": "{{name}}",
        "namespace": "SchemaNamespace",
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("[]")]
    public Task Verify_Name_Diagnostic(string name) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "enum",
        "name": {{name}},
        "namespace": "SchemaNamespace",
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"Schema1.Enum.Namespace\""), InlineData("\"schema2.enum.namespace\"")]
    public Task Verify_Namespace(string @namespace) => TestHelper.VerifySourceCode($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": {{@namespace}},
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("\"\""), InlineData("[]")]
    public Task Verify_Namespace_Diagnostic(string @namespace) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": {{@namespace}},
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    public Task Verify_Documentation(string doc) => TestHelper.VerifySourceCode($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "doc": {{doc}},
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("[]")]
    public Task Verify_Documentation_Diagnostic(string doc) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "doc": {{doc}},
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    public Task Verify_Aliases(string aliases) => TestHelper.VerifySourceCode($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "aliases": {{aliases}},
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("{}")]
    public Task Verify_Aliases_Diagnostic(string aliases) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "aliases": {{aliases}},
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("[]"), InlineData("[\"A\", \"B\"]")]
    public Task Verify_Symbols(string symbols) => TestHelper.VerifySourceCode($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "symbols": {{symbols}}
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("{}")]
    public Task Verify_Symbols_Diagnostic(string symbols) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "symbols": {{symbols}}
    }
    """);
}
