namespace AvroSourceGenerator.Tests;

public sealed class ErrorTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("file"), InlineData("")]
    public Task Verify_AccessModifier_Local(string accessModifier)
    {
        var schema = """
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "fields": []
        }
        """;

        var source = $$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro]
        {{accessModifier}} partial class Error;
        """";

        return TestHelper.VerifySourceCode(schema, source);
    }

    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("invalid")]
    public Task Verify_AccessModifier_Global(string accessModifier)
    {
        var schema = """
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "fields": []
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
    [InlineData("Error"), InlineData("exception"), InlineData("throw")]
    public Task Verify_Name(string name) => TestHelper.VerifySourceCode($$"""
    {
        "type": "error",
        "name": "{{name}}",
        "namespace": "SchemaNamespace",
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("[]")]
    public Task Verify_Name_Diagnostic(string name) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "error",
        "name": {{name}},
        "namespace": "SchemaNamespace",
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"Schema1.Throw.Namespace\""), InlineData("\"schema2.throw.namespace\"")]
    public Task Verify_Namespace(string @namespace) => TestHelper.VerifySourceCode($$"""
    {
        "type": "error",
        "name": "Error",
        "namespace": {{@namespace}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("\"\""), InlineData("[]")]
    public Task Verify_Namespace_Diagnostic(string @namespace) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "error",
        "name": "Error",
        "namespace": {{@namespace}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    public Task Verify_Documentation(string doc) => TestHelper.VerifySourceCode($$"""
    {
        "type": "error",
        "name": "Error",
        "namespace": "SchemaNamespace",
        "doc": {{doc}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("[]")]
    public Task Verify_Documentation_Diagnostic(string doc) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "error",
        "name": "Error",
        "namespace": "SchemaNamespace",
        "doc": {{doc}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    public Task Verify_Aliases(string aliases) => TestHelper.VerifySourceCode($$"""
    {
        "type": "error",
        "name": "Error",
        "namespace": "SchemaNamespace",
        "aliases": {{aliases}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("{}")]
    public Task Verify_Aliases_Diagnostic(string aliases) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "error",
        "name": "Error",
        "namespace": "SchemaNamespace",
        "aliases": {{aliases}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("{}")]
    public Task Verify_Fields_Diagnostic(string fields) => TestHelper.VerifyDiagnostic($$""""
    {
        "type": "error",
        "namespace": "SchemaNamespace",
        "name": "Error",
        "fields": {{fields}}
    }
    """");

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures_Local(string languageFeatures)
    {
        var schema = """
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "fields": [
                {
                    "name": "ErrorCode",
                    "type": "int"
                },
                {
                    "name": "ErrorText",
                    "type": "string"
                }
            ]
        }
        """;

        var source = $$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(LanguageFeatures = LanguageFeatures.{{languageFeatures}})]
        public partial class Error;
        """";

        return TestHelper.VerifySourceCode(schema, source);
    }

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures_Global(string languageFeatures)
    {
        var schema = """
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "fields": [
                {
                    "name": "ErrorCode",
                    "type": "int"
                },
                {
                    "name": "ErrorText",
                    "type": "string"
                }]
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
