using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Apache.Snapshots;

public sealed class NestedCollectionTests
{
    [Fact]
    public Task Verify_nested_map_error_conversion() => Snapshot.Schema(
        """
        {
          "type": "error",
          "name": "NestedMapError",
          "namespace": "NestedRegression",
          "fields": [
            {
              "name": "Maps",
              "type": { "type": "map", "values": { "type": "map", "values": "float" } }
            },
            {
              "name": "NullableArrays",
              "type": { "type": "map", "values": ["null", { "type": "array", "items": "float" }] }
            }
          ]
        }
        """,
        config => config with
        {
            AvroLibrary = "Apache",
            LanguageVersion = LanguageVersion.CSharp10,
            LanguageFeatures = "CSharp10",
            RecordDeclaration = "class",
            PreviewFeatures = "None",
        });

    [Fact]
    public Task Verify_nested_array_conversion() => Snapshot.Schema(
        """
        {
          "type": "record",
          "name": "NestedArrayRecord",
          "namespace": "NestedRegression",
          "fields": [
            {
              "name": "Arrays",
              "type": { "type": "array", "items": { "type": "array", "items": "float" } }
            },
            {
              "name": "NullableArrays",
              "type": ["null", { "type": "array", "items": { "type": "array", "items": "float" } }],
              "default": null
            }
          ]
        }
        """,
        config => config with
        {
            AvroLibrary = "Apache",
            LanguageVersion = LanguageVersion.CSharp8,
            LanguageFeatures = "CSharp8",
            RecordDeclaration = "class",
            PreviewFeatures = "None",
        });
}
