using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class RecordTests
{
    [Theory]
    [InlineData("Record"), InlineData("exception"), InlineData("throw")]
    public Task Verify_Name(string name) => TestHelper.VerifySourceCode($$"""
    {
        "type": "record",
        "name": "{{name}}",
        "namespace": "SchemaNamespace",
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("[]")]
    public Task Verify_Name_Diagnostic(string name) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "record",
        "name": {{name}},
        "namespace": "SchemaNamespace",
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Schema1.Throw.Namespace\""), InlineData("\"schema2.throw.namespace\"")]
    public Task Verify_Namespace(string @namespace) => TestHelper.VerifySourceCode($$"""
    {
        "type": "record",
        "name": "Record",
        "namespace": {{@namespace}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("[]")]
    public Task Verify_Namespace_Diagnostic(string @namespace) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "record",
        "name": "Record",
        "namespace": {{@namespace}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    public Task Verify_Documentation(string doc) => TestHelper.VerifySourceCode($$"""
    {
        "type": "record",
        "name": "Record",
        "namespace": "SchemaNamespace",
        "doc": {{doc}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("[]")]
    public Task Verify_Documentation_Diagnostic(string doc) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "record",
        "name": "Record",
        "namespace": "SchemaNamespace",
        "doc": {{doc}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    public Task Verify_Aliases(string aliases) => TestHelper.VerifySourceCode($$"""
    {
        "type": "record",
        "name": "Record",
        "namespace": "SchemaNamespace",
        "aliases": {{aliases}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("{}")]
    public Task Verify_Aliases_Diagnostic(string aliases) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "record",
        "name": "Record",
        "namespace": "SchemaNamespace",
        "aliases": {{aliases}},
        "fields": []
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("{}")]
    public Task Verify_Fields_Diagnostic(string fields) => TestHelper.VerifyDiagnostic($$""""
    {
        "type": "record",
        "namespace": "SchemaNamespace",
        "name": "Record",
        "fields": {{fields}}
    }
    """");

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures_Local(string languageFeatures)
    {
        var schema = """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": [
                {
                    "name": "Value1",
                    "type": "int"
                },
                {
                    "name": "Value2",
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
        public partial class Record;
        """";

        return TestHelper.VerifySourceCode(schema, source);
    }

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures_Global(string languageFeatures)
    {
        var schema = """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": [
                {
                    "name": "Value1",
                    "type": "int"
                },
                {
                    "name": "Value2",
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

    [Theory]
    [InlineData("record"), InlineData("class")]
    public Task Verify_RecordDeclaration_Local(string recordDeclaration)
    {
        var schema = """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": []
        }
        """;

        var source = $$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro]
        public partial {{recordDeclaration}} Record;
        """";

        return TestHelper.VerifySourceCode(schema, source);
    }

    [Theory]
    [InlineData("record"), InlineData("class")]
    public Task Verify_RecordDeclaration_Global(string recordDeclaration)
    {
        var schema = """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": []
        }
        """;

        var config = ProjectConfig.Default with
        {
            GlobalOptions = new Dictionary<string, string>
            {
                ["AvroSourceGeneratorRecordDeclaration"] = recordDeclaration
            }
        };

        return TestHelper.VerifySourceCode(schema, default, config);
    }
}
