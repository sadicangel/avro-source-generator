using System.Diagnostics.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class SchemaRegistryDuplicateResolutionTests
{
    [Fact]
    public Task Verify() => Snapshot.Files(
        [ProjectFile.Schema(Schema1), ProjectFile.Schema(Schema2)],
        config => config with
        {
            DuplicateResolution = "Ignore",
        });

    [Fact]
    public Task Diagnostic() =>
        Snapshot.Diagnostic([ProjectFile.Schema(Schema1), ProjectFile.Schema(Schema2)]);

    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string Schema1 = """
        {
          "type": "record",
          "name": "SharedRecord",
          "namespace": "Demo",
          "fields": []
        }
        """;

    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string Schema2 = """
        {
          "type": "record",
          "name": "Container",
          "namespace": "Demo",
          "fields": [
            {
              "name": "InlineShared",
              "type": {
                "type": "record",
                "name": "SharedRecord",
                "namespace": "Demo",
                "fields": []
              }
            },
            {
              "name": "ReferencedShared",
              "type": "SharedRecord"
            }
          ]
        }
        """;
}
