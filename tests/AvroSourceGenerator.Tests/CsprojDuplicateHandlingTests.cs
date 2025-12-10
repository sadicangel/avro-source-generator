using System.Diagnostics.CodeAnalysis;

namespace AvroSourceGenerator.Tests;

public sealed class CsprojDuplicateResolutionTests
{
    [Fact]
    public Task Verify() => VerifySourceCode([Schema1, Schema2], new ProjectConfig { DuplicateResolution = "Ignore" });

    [Fact]
    public Task Diagnostic() => VerifyDiagnostic([Schema1, Schema2]);

    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string Schema1 = """
        {
            "type": "record",
            "name": "Schema1",
            "namespace": "Schema1Namespace",
            "fields": [
                {
                    "name": "Record",
                    "type": {
                        "type": "record",
                        "name": "Record",
                        "namespace": "RecordNamespace",
                        "fields": []
                    }
                }
            ]
        }
        """;

    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string Schema2 = """
        {
            "type": "record",
            "name": "Schema2",
            "namespace": "Schema2Namespace",
            "fields": [
                {
                    "name": "Record",
                    "type": {
                        "type": "record",
                        "name": "Record",
                        "namespace": "RecordNamespace",
                        "fields": []
                    }
                }
            ]
        }
        """;
}
