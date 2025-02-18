namespace AvroSourceGenerator.Tests;

public class AvroEnumTests
{
    [Theory]
    [InlineData("EnumName"), InlineData("enum_name"), InlineData("enum")]
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
    public Task VerifyNameDiagnostic(string name) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "enum",
        "name": {{name}},
        "namespace": "SchemaNamespace",
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"Schema1.Enum.Namespace\""), InlineData("\"schema2.enum.namespace\"")]
    public Task VerifyNamespace(string @namespace) => TestHelper.VerifySourceCode($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": {{@namespace}},
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("\"\""), InlineData("[]")]
    public Task VerifyNamespaceDiagnostic(string @namespace) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": {{@namespace}},
        "symbols": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    public Task VerifyDocumentation(string doc) => TestHelper.VerifySourceCode($$"""
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
    public Task VerifyDocumentationDiagnostic(string doc) => TestHelper.VerifyDiagnostic($$"""
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
    public Task VerifyAliases(string aliases) => TestHelper.VerifySourceCode($$"""
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
    public Task VerifyAliasesDiagnostic(string aliases) => TestHelper.VerifyDiagnostic($$"""
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
    public Task VerifySymbols(string symbols) => TestHelper.VerifySourceCode($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "symbols": {{symbols}}
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("{}")]
    public Task VerifySymbolsDiagnostic(string symbols) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "symbols": {{symbols}}
    }
    """);
}
