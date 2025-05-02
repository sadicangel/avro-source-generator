using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class RecordTests
{
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
