using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class NestedCollectionGenerationTests
{
    [Theory]
    [InlineData(LanguageVersion.CSharp7_3, "CSharp7_3", "record")]
    [InlineData(LanguageVersion.CSharp12, "CSharp12", "record")]
    [InlineData(LanguageVersion.CSharp7_3, "CSharp7_3", "error")]
    [InlineData(LanguageVersion.CSharp12, "CSharp12", "error")]
    public void Collection_helpers_do_not_conflict_with_schema_fields(LanguageVersion version, string features, string schemaType)
    {
        var schema = $$"""
            {
              "type": "{{schemaType}}",
              "name": "CollectionFields",
              "fields": [
                { "name": "ConvertArray", "type": { "type": "array", "items": { "type": "array", "items": "float" } } },
                { "name": "ConvertMap", "type": { "type": "map", "values": { "type": "map", "values": "float" } } }
              ]
            }
            """;
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(schema)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig
            {
                AvroLibrary = "Apache",
                LanguageVersion = version,
                LanguageFeatures = features,
                RecordDeclaration = "class",
                PreviewFeatures = "None",
            });

        input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp7_3, "CSharp7_3", "class", "None", "record")]
    [InlineData(LanguageVersion.CSharp8, "CSharp8", "class", "None", "record")]
    [InlineData(LanguageVersion.CSharp9, "CSharp9", "record", "None", "record")]
    [InlineData(LanguageVersion.CSharp10, "CSharp10", "record", "None", "record")]
    [InlineData(LanguageVersion.CSharp11, "CSharp11", "record", "None", "record")]
    [InlineData(LanguageVersion.CSharp12, "CSharp12", "record", "None", "record")]
    [InlineData(LanguageVersion.Preview, "Latest", "record", "Unions", "record")]
    [InlineData(LanguageVersion.CSharp7_3, "CSharp7_3", "class", "None", "error")]
    [InlineData(LanguageVersion.CSharp8, "CSharp8", "class", "None", "error")]
    [InlineData(LanguageVersion.CSharp9, "CSharp9", "class", "None", "error")]
    [InlineData(LanguageVersion.CSharp10, "CSharp10", "class", "None", "error")]
    [InlineData(LanguageVersion.CSharp11, "CSharp11", "class", "None", "error")]
    [InlineData(LanguageVersion.CSharp12, "CSharp12", "class", "None", "error")]
    [InlineData(LanguageVersion.Preview, "Latest", "class", "Unions", "error")]
    public void Nested_collection_conversions_compile_without_changing_properties(
        LanguageVersion version, string features, string declaration, string preview, string schemaType)
    {
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(Schema.Replace("\"type\": \"record\"", $"\"type\": \"{schemaType}\""))],
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

        var source = driver.GetRunResult().GeneratedTrees.Single(tree => tree.FilePath.EndsWith("CollectionRecord.Avro.g.cs"));
        Assert.DoesNotContain(source.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
            .OfType<LocalFunctionStatementSyntax>(), function => function.Identifier.ValueText is "__ApacheConvertArray" or "__ApacheConvertMap");
        var helpers = source.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(method => method.Identifier.ValueText is "__ApacheConvertArray" or "__ApacheConvertMap").ToArray();
        Assert.Equal(["__ApacheConvertArray", "__ApacheConvertMap"], helpers.Select(function => function.Identifier.ValueText));
        Assert.All(helpers, helper =>
        {
            Assert.True(helper.Modifiers.Any(SyntaxKind.PrivateKeyword));
            Assert.True(helper.Modifiers.Any(SyntaxKind.StaticKeyword));
            Assert.Equal("CollectionRecord", Assert.IsAssignableFrom<TypeDeclarationSyntax>(helper.Parent).Identifier.ValueText);
        });

        var converters = source.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
            .OfType<SimpleLambdaExpressionSyntax>().ToArray();
        Assert.NotEmpty(converters);
        Assert.All(converters, converter =>
        {
            if (version == LanguageVersion.CSharp7_3)
                Assert.StartsWith("item_", converter.Parameter.Identifier.ValueText);
            else
                Assert.Equal("item", converter.Parameter.Identifier.ValueText);
        });

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
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void Nested_array_and_bytes_union_compiles_in_either_branch_order(bool bytesFirst, bool allowsNull, bool nullLast)
    {
        const string array = """{"type":"array","items":{"type":"array","items":"float"}}""";
        var branches = bytesFirst ? $"\"bytes\", {array}" : $"{array}, \"bytes\"";
        if (allowsNull)
        {
            branches = nullLast ? $"{branches}, \"null\"" : $"\"null\", {branches}";
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

    [Theory]
    [InlineData("\"int\"", false, false, false)]
    [InlineData("""{"type":"array","items":"float"}""", false, false, false)]
    [InlineData("""{"type":"map","values":"float"}""", false, false, false)]
    [InlineData("""{"type":"array","items":["null","int"]}""", false, false, false)]
    [InlineData("""["null",{"type":"array","items":"float"}]""", false, false, false)]
    [InlineData("""[{"type":"map","values":"float"},"null"]""", false, false, false)]
    [InlineData("""["string",{"type":"array","items":"float"}]""", true, false, false)]
    [InlineData("""["string",{"type":"map","values":"float"}]""", true, false, false)]
    [InlineData("""[{"type":"array","items":"float"},{"type":"map","values":"float"}]""", true, false, false)]
    [InlineData("""{"type":"array","items":{"type":"array","items":"float"}}""", false, true, false)]
    [InlineData("""["null",{"type":"array","items":{"type":"array","items":"float"}}]""", false, true, false)]
    [InlineData("""[{"type":"array","items":{"type":"array","items":"float"}},"null"]""", false, true, false)]
    [InlineData("""[{"type":"array","items":{"type":"array","items":"float"}}]""", false, true, false)]
    [InlineData("""{"type":"array","items":["null",{"type":"array","items":"float"}]}""", false, true, false)]
    [InlineData("""{"type":"array","items":[{"type":"array","items":"float"},"null"]}""", false, true, false)]
    [InlineData("""{"type":"map","values":["null",{"type":"map","values":"float"}]}""", false, false, true)]
    [InlineData("""{"type":"map","values":[{"type":"map","values":"float"},"null"]}""", false, false, true)]
    [InlineData("""{"type":"array","items":{"type":"map","values":"float"}}""", false, true, true)]
    [InlineData("""{"type":"map","values":{"type":"array","items":"float"}}""", false, true, true)]
    [InlineData("""["string",{"type":"array","items":{"type":"array","items":"float"}}]""", true, true, false)]
    [InlineData("""[{"type":"array","items":"float"},{"type":"map","values":{"type":"map","values":"float"}}]""", true, false, true)]
    [InlineData("""[{"type":"map","values":"float"},{"type":"array","items":{"type":"array","items":"float"}}]""", true, true, false)]
    [InlineData("""["string",{"type":"array","items":{"type":"array","items":"float"}}]""", false, false, false)]
    [InlineData("""{"type":"array","items":["string",{"type":"array","items":"float"}]}""", false, false, false)]
    public void Put_emits_each_required_helper_once(string fieldType, bool nativeUnions, bool needsArray, bool needsMap)
    {
        var schema = $$"""
            {
              "type": "record",
              "name": "HelperRecord",
              "fields": [
                { "name": "First", "type": {{fieldType}} },
                { "name": "Second", "type": {{fieldType}} }
              ]
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
                PreviewFeatures = nativeUnions ? "Unions" : "None",
            });

        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));

        var source = driver.GetRunResult().GeneratedTrees.Single(tree => tree.FilePath.EndsWith("HelperRecord.Avro.g.cs"));
        var helpers = source.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
            .OfType<MethodDeclarationSyntax>().ToArray();
        Assert.Equal(needsArray ? 1 : 0, helpers.Count(helper => helper.Identifier.ValueText == "__ApacheConvertArray"));
        Assert.Equal(needsMap ? 1 : 0, helpers.Count(helper => helper.Identifier.ValueText == "__ApacheConvertMap"));
        Assert.DoesNotContain("global::System.Linq.Enumerable", source.ToString());
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp7_3, "CSharp7_3", false)]
    [InlineData(LanguageVersion.CSharp8, "CSharp8", false)]
    [InlineData(LanguageVersion.CSharp10, "CSharp10", false)]
    [InlineData(LanguageVersion.CSharp12, "CSharp12", false)]
    [InlineData(LanguageVersion.Preview, "Latest", true)]
    public void Flat_collections_use_casts_inside_and_outside_unions(LanguageVersion version, string features, bool nativeUnions)
    {
        const string array = """{"type":"array","items":"float"}""";
        const string map = """{"type":"map","values":"float"}""";
        var arrayType = nativeUnions ? $"[\"null\", \"bytes\", {array}]" : $"[\"null\", {array}]";
        var mapType = nativeUnions ? $"[\"null\", \"string\", {map}]" : $"[\"null\", {map}]";
        var schema = $$"""
            {
              "type": "record",
              "name": "FlatCollectionRecord",
              "fields": [
                { "name": "Array", "type": {{arrayType}} },
                { "name": "Map", "type": {{mapType}} },
                { "name": "RequiredArray", "type": {{array}} },
                { "name": "RequiredMap", "type": {{map}} }
              ]
            }
            """;
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(schema)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig
            {
                AvroLibrary = "Apache",
                LanguageVersion = version,
                LanguageFeatures = features,
                RecordDeclaration = "class",
                PreviewFeatures = nativeUnions ? "Unions" : "None",
            });

        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));

        var source = driver.GetRunResult().GeneratedTrees.Single(tree => tree.FilePath.EndsWith("FlatCollectionRecord.Avro.g.cs"));
        Assert.DoesNotContain("__ApacheConvertArray", source.ToString());
        Assert.DoesNotContain("__ApacheConvertMap", source.ToString());

        using var assemblyStream = new MemoryStream();
        var emitted = compilation.Emit(assemblyStream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        var assembly = System.Reflection.Assembly.Load(assemblyStream.ToArray());
        var model = assembly.GetType("FlatCollectionRecord", throwOnError: true)!;
        var record = (Avro.Specific.ISpecificRecord)Activator.CreateInstance(model)!;

        var list = new List<float> { 1.25f };
        var dictionary = new Dictionary<string, float> { ["value"] = 1.25f };
        record.Put(0, list);
        Assert.Same(list, record.Get(0));
        record.Put(1, dictionary);
        Assert.Same(dictionary, record.Get(1));
        record.Put(2, list);
        Assert.Same(list, record.Get(2));
        record.Put(3, dictionary);
        Assert.Same(dictionary, record.Get(3));
        record.Put(0, null);
        Assert.Null(record.Get(0));
        record.Put(1, null);
        Assert.Null(record.Get(1));
        record.Put(2, null);
        Assert.Null(record.Get(2));
        record.Put(3, null);
        Assert.Null(record.Get(3));

        if (nativeUnions)
        {
            byte[] bytes = [0, 1, 255];
            record.Put(0, bytes);
            Assert.Same(bytes, record.Get(0));
            record.Put(1, "scalar");
            Assert.Equal("scalar", record.Get(1));
        }
    }

    [Fact]
    public void Named_records_normalize_their_own_fields()
    {
        const string schema = """
            {
              "type": "record",
              "name": "Parent",
              "fields": [
                { "name": "Child", "type": {
                  "type": "record",
                  "name": "Child",
                  "fields": [{ "name": "Arrays", "type": { "type": "array", "items": { "type": "array", "items": "float" } } }]
                } },
                { "name": "Children", "type": { "type": "array", "items": "Child" } }
              ]
            }
            """;
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(schema)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig { AvroLibrary = "Apache", LanguageFeatures = "CSharp8", PreviewFeatures = "None" });

        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));

        var sources = driver.GetRunResult().GeneratedTrees;
        var parentConversions = sources.Single(tree => tree.FilePath.EndsWith("Parent.Avro.g.cs"))
            .GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is GenericNameSyntax { Identifier.ValueText: "__ApacheConvertArray" });
        Assert.Empty(parentConversions);
        Assert.Contains("__ApacheConvertArray", sources.Single(tree => tree.FilePath.EndsWith("Child.Avro.g.cs")).ToString());
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp7_3, "CSharp7_3", "record")]
    [InlineData(LanguageVersion.CSharp8, "CSharp8", "record")]
    [InlineData(LanguageVersion.CSharp10, "CSharp10", "record")]
    [InlineData(LanguageVersion.CSharp12, "CSharp12", "record")]
    [InlineData(LanguageVersion.CSharp7_3, "CSharp7_3", "error")]
    [InlineData(LanguageVersion.CSharp8, "CSharp8", "error")]
    [InlineData(LanguageVersion.CSharp10, "CSharp10", "error")]
    [InlineData(LanguageVersion.CSharp12, "CSharp12", "error")]
    public void Collection_helpers_handle_nulls_for_all_setters(LanguageVersion version, string features, string schemaType)
    {
        var schema = $$"""
            {
              "type": "{{schemaType}}",
              "name": "NullHandlingRecord",
              "fields": [
                { "name": "RequiredArrays", "type": { "type": "array", "items": { "type": "array", "items": "float" } } },
                { "name": "NullableArrays", "type": ["null", { "type": "array", "items": { "type": "array", "items": "float" } }] },
                { "name": "RequiredMaps", "type": { "type": "map", "values": { "type": "map", "values": "float" } } },
                { "name": "NullableMaps", "type": ["null", { "type": "map", "values": { "type": "map", "values": "float" } }] },
                { "name": "NullableItems", "type": { "type": "array", "items": ["null", { "type": "array", "items": "float" }] } },
                { "name": "NullableValues", "type": { "type": "map", "values": ["null", { "type": "map", "values": "float" }] } }
              ]
            }
            """;
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(schema)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig
            {
                AvroLibrary = "Apache",
                LanguageVersion = version,
                LanguageFeatures = features,
                RecordDeclaration = "class",
                PreviewFeatures = "None",
            });

        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));

        var source = Assert.Single(driver.GetRunResult().GeneratedTrees);
        var dispatch = source.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Put")
            .DescendantNodes().OfType<SwitchStatementSyntax>().Single();
        Assert.Empty(dispatch.DescendantNodes().OfType<ConditionalExpressionSyntax>());

        using var assemblyStream = new MemoryStream();
        var emitted = compilation.Emit(assemblyStream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        var assembly = System.Reflection.Assembly.Load(assemblyStream.ToArray());
        var model = assembly.GetType("NullHandlingRecord", throwOnError: true)!;
        var record = (Avro.Specific.ISpecificRecord)Activator.CreateInstance(model)!;

        for (var fieldPos = 0; fieldPos < 4; fieldPos++)
        {
            record.Put(fieldPos, null);
            Assert.Null(record.Get(fieldPos));
        }

        object?[] items = [null, new float[] { 1.25f }];
        record.Put(4, items);
        var convertedItems = Assert.IsAssignableFrom<System.Collections.IList>(record.Get(4));
        Assert.Null(convertedItems[0]);
        Assert.Equal([1.25f], Assert.IsType<List<float>>(convertedItems[1]));
        record.Put(0, items);
        var requiredItems = Assert.IsType<List<List<float>>>(record.Get(0));
        Assert.Null(requiredItems[0]);
        Assert.Equal([1.25f], requiredItems[1]);

        var values = new System.Collections.Hashtable
        {
            { "null", null },
            { "populated", new System.Collections.Hashtable { { "value", 1.25f } } },
        };
        record.Put(5, values);
        var convertedValues = Assert.IsAssignableFrom<System.Collections.IDictionary>(record.Get(5));
        Assert.Null(convertedValues["null"]);
        Assert.Equal(1.25f, Assert.IsType<Dictionary<string, float>>(convertedValues["populated"])["value"]);
        record.Put(2, values);
        var requiredValues = Assert.IsType<Dictionary<string, Dictionary<string, float>>>(record.Get(2));
        Assert.Null(requiredValues["null"]);
        Assert.Equal(1.25f, requiredValues["populated"]["value"]);
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
