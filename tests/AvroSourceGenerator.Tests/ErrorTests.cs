using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class ErrorTests
{
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
