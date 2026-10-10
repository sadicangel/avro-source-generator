using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Apache.Snapshots;

public sealed class NestedCollectionTests
{
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
