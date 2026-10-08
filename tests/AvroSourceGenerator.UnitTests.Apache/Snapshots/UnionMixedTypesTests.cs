using AvroSourceGenerator.Compiler;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Apache.Snapshots;

public sealed class UnionMixedTypesTests
{
    [Theory]
    [InlineData(nameof(LanguageFeatures.CSharp14))]
    [InlineData(nameof(LanguageFeatures.CSharp15))]
    public Task Verify(string languageFeatures) => Snapshot.Schema(
        """
        {
          "type": "record", "name": "Envelope", "namespace": "Demo",
          "fields": [
            { "name": "choice", "type": [
              "null", "string",
              { "type": "fixed", "name": "Id", "size": 16 },
              { "type": "record", "name": "Card", "fields": [] },
              { "type": "enum", "name": "Status", "symbols": ["Pending", "Approved"] }
            ] },
            { "name": "reference", "type": ["null", "Id", "Card", "string"] },
            { "name": "collection", "type": [
              { "type": "array", "items": "string" },
              { "type": "map", "values": "long" }
            ] },
            { "name": "logical", "type": [
              { "type": "int", "logicalType": "date" }, "long"
            ] }
          ]
        }
        """,
        config => config with
        {
            LanguageFeatures = languageFeatures,
            PreviewFeatures = languageFeatures == nameof(LanguageFeatures.CSharp15) ? "Unions" : "None",
            LanguageVersion = languageFeatures == nameof(LanguageFeatures.CSharp15) ? LanguageVersion.Preview : LanguageVersion.CSharp14
        }).UseParameters(languageFeatures);
}
