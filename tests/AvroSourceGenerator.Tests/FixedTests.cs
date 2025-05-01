using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class FixedTests
{
    [Theory]
    [InlineData("Fixed"), InlineData("exception"), InlineData("throw")]
    public Task Verify_Name(string name) => TestHelper.VerifySourceCode($$"""
    {
        "type": "fixed",
        "name": "{{name}}",
        "namespace": "SchemaNamespace",
        "size": 16
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("[]")]
    public Task Verify_Name_Diagnostic(string name) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "fixed",
        "name": {{name}},
        "namespace": "SchemaNamespace",
        "size": 16
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Schema1.Throw.Namespace\""), InlineData("\"schema2.throw.namespace\"")]
    public Task Verify_Namespace(string @namespace) => TestHelper.VerifySourceCode($$"""
    {
        "type": "fixed",
        "name": "Fixed",
        "namespace": {{@namespace}},
        "size": 16
    }
    """);

    [Theory]
    [InlineData("[]")]
    public Task Verify_Namespace_Diagnostic(string @namespace) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "fixed",
        "name": "Fixed",
        "namespace": {{@namespace}},
        "size": 16
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    public Task Verify_Documentation(string doc) => TestHelper.VerifySourceCode($$"""
    {
        "type": "fixed",
        "name": "Fixed",
        "namespace": "SchemaNamespace",
        "doc": {{doc}},
        "size": 16
    }
    """);

    [Theory]
    [InlineData("[]")]
    public Task Verify_Documentation_Diagnostic(string doc) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "fixed",
        "name": "Fixed",
        "namespace": "SchemaNamespace",
        "doc": {{doc}},
        "size": 16
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    public Task Verify_Aliases(string aliases) => TestHelper.VerifySourceCode($$"""
    {
        "type": "fixed",
        "name": "Fixed",
        "namespace": "SchemaNamespace",
        "aliases": {{aliases}},
        "size": 16
    }
    """);

    [Theory]
    [InlineData("{}")]
    public Task Verify_Aliases_Diagnostic(string aliases) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "fixed",
        "name": "Fixed",
        "namespace": "SchemaNamespace",
        "aliases": {{aliases}},
        "size": 16
    }
    """);

    [Theory]
    [InlineData("16")]
    public Task Verify_Size(string size) => TestHelper.VerifySourceCode($$"""
    {
        "type": "fixed",
        "namespace": "SchemaNamespace",
        "name": "Fixed",
        "size": {{size}}
    }
    """);

    [Theory]
    [InlineData("0"), InlineData("-1"), InlineData("1.1"), InlineData("null"), InlineData("\"A\"")]
    public Task Verify_Size_Diagnostic(string size) => TestHelper.VerifyDiagnostic($$"""
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "size": {{size}}
        }
        """);

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures_Local(string languageFeatures)
    {
        var schema = """
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "size": 16
        }
        """;

        var source = $$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(LanguageFeatures = LanguageFeatures.{{languageFeatures}})]
        public partial class Fixed;
        """";

        return TestHelper.VerifySourceCode(schema, source);
    }

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures_Global(string languageFeatures)
    {
        var schema = """
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "size": 16
        }
        """;

        var config = ProjectConfig.Default with
        {
            GlobalOptions = new Dictionary<string, string>
            {
                ["AvroSourceGeneratorLanguageFeatures"] = languageFeatures
            }
        };

        return TestHelper.VerifySourceCode(schema, default, config);
    }
}
