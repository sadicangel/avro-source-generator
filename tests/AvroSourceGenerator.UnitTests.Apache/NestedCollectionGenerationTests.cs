using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class NestedCollectionGenerationTests
{
    [Theory]
    [InlineData(LanguageVersion.CSharp7_3, "CSharp7_3", "class", "None")]
    [InlineData(LanguageVersion.CSharp8, "CSharp8", "class", "None")]
    [InlineData(LanguageVersion.CSharp9, "CSharp9", "record", "None")]
    [InlineData(LanguageVersion.CSharp10, "CSharp10", "record", "None")]
    [InlineData(LanguageVersion.CSharp11, "CSharp11", "record", "None")]
    [InlineData(LanguageVersion.CSharp12, "CSharp12", "record", "None")]
    [InlineData(LanguageVersion.Preview, "Latest", "record", "Unions")]
    public void Nested_collection_conversions_compile_without_changing_properties(
        LanguageVersion version, string features, string declaration, string preview)
    {
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(Schema)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig
            {
                AvroLibrary = "Apache",
                LanguageVersion = version,
                LanguageFeatures = features,
                RecordDeclaration = declaration,
                PreviewFeatures = preview,
            });

        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));

        var model = compilation.GetTypeByMetadataName("NestedRegression.CollectionRecord");
        Assert.NotNull(model);

        var nullableSuffix = version == LanguageVersion.CSharp7_3 ? "" : "?";
        var property = Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(model.GetMembers("Arrays")));
        var expectedType = "System.Collections.Generic.List<System.Collections.Generic.List<float>>" + nullableSuffix;
        Assert.Equal(expectedType, property.Type.ToDisplayString());

        var triple = Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(model.GetMembers("TripleArrays")));
        var expectedTripleType = "System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<int>>>" + nullableSuffix;
        Assert.Equal(expectedTripleType, triple.Type.ToDisplayString());

        driver = driver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        var outputs = driver.GetRunResult().Results.Single().TrackedOutputSteps
            .SelectMany(step => step.Value)
            .SelectMany(step => step.Outputs);

        Assert.All(outputs,
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Nested_array_and_bytes_union_compiles_in_either_branch_order(bool bytesFirst, bool allowsNull)
    {
        const string array = """{"type":"array","items":{"type":"array","items":"float"}}""";
        var branches = bytesFirst ? $"\"bytes\", {array}" : $"{array}, \"bytes\"";
        if (allowsNull)
        {
            branches = $"\"null\", {branches}";
        }

        var schema = $$"""
            {
              "type": "record",
              "name": "ArrayAndBytesRecord",
              "namespace": "NestedRegression",
              "fields": [{ "name": "Value", "type": [{{branches}}] }]
            }
            """;
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(schema)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig
            {
                AvroLibrary = "Apache",
                LanguageVersion = LanguageVersion.Preview,
                LanguageFeatures = "Latest",
                RecordDeclaration = "class",
                PreviewFeatures = "Unions",
            });

        input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));
    }

    private const string Schema = """
        {
          "type": "record",
          "name": "CollectionRecord",
          "namespace": "NestedRegression",
          "fields": [
            {
              "name": "Arrays",
              "type": [
                "null",
                {
                  "type": "array",
                  "items": {
                    "type": "array",
                    "items": "float"
                  }
                }
              ],
              "default": null
            },
            {
              "name": "Maps",
              "type": [
                "null",
                {
                  "type": "map",
                  "values": {
                    "type": "map",
                    "values": "string"
                  }
                }
              ],
              "default": null
            },
            {
              "name": "Mixed",
              "type": [
                "null",
                "string",
                {
                  "type": "array",
                  "items": {
                    "type": "array",
                    "items": "float"
                  }
                },
                {
                  "type": "map",
                  "values": {
                    "type": "array",
                    "items": "float"
                  }
                }
              ],
              "default": null
            },
            {
              "name": "TripleArrays",
              "type": [
                "null",
                {
                  "type": "array",
                  "items": {
                    "type": "array",
                    "items": {
                      "type": "array",
                      "items": "int"
                    }
                  }
                }
              ],
              "default": null
            },
            {
              "name": "QuadrupleArrays",
              "type": [
                "null",
                {
                  "type": "array",
                  "items": {
                    "type": "array",
                    "items": {
                      "type": "array",
                      "items": {
                        "type": "array",
                        "items": "int"
                      }
                    }
                  }
                }
              ],
              "default": null
            },
            {
              "name": "NullableItems",
              "type": {
                "type": "array",
                "items": [
                  "null",
                  {
                    "type": "array",
                    "items": "float"
                  }
                ]
              }
            }
          ]
        }
        """;
}
