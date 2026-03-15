using System.Diagnostics.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Chr.Snapshots;

public sealed class CsprojDuplicateResolutionTests
{
    [Fact]
    public Task Verify() => Snapshot.Files([ProjectFile.Schema(Schema1), ProjectFile.Schema(Schema2)], config => config with { DuplicateResolution = "Ignore" });

    [Fact]
    public Task Diagnostic() => Snapshot.Diagnostic([ProjectFile.Schema(Schema1), ProjectFile.Schema(Schema2)]);

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
