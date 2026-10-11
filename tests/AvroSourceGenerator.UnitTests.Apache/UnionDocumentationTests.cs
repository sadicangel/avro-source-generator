using System.Xml.Linq;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class UnionDocumentationTests
{
    [Theory]
    [InlineData("""{"type":"array","items":"int","logicalType":"custom-collection"}""",
        "<see cref=\"global::System.Collections.Generic.List{T}\">List</see>&lt;<see cref=\"int\"/>&gt;")]
    [InlineData("""{"type":"map","values":"string","logicalType":"custom-collection"}""",
        "<see cref=\"global::System.Collections.Generic.Dictionary{TKey, TValue}\">Dictionary</see>&lt;<see cref=\"string\"/>, <see cref=\"string\"/>&gt;")]
    [InlineData("""{"type":"map","values":{"type":"array","items":"int"},"logicalType":"custom-collection"}""",
        "<see cref=\"global::System.Collections.Generic.Dictionary{TKey, TValue}\">Dictionary</see>&lt;<see cref=\"string\"/>, <see cref=\"global::System.Collections.Generic.List{T}\">List</see>&lt;<see cref=\"int\"/>&gt;&gt;")]
    public void Unknown_logical_collection_types_have_valid_documentation(string collection, string expectedElement)
    {
        var schema = $$"""
            {
              "type":"record","name":"Envelope",
              "fields":[{"name":"Choice","type":["string",{{collection}}]}]
            }
            """;
        var cancellationToken = TestContext.Current.CancellationToken;
        var file = AvroFile.Parse(new SourceText("envelope.avsc", schema),
            new AvroParseOptions(GenerationTarget.Apache, LanguageFeatures.CSharp15), cancellationToken);
        Assert.Empty(file.Diagnostics);
        var symbols = SymbolTable.FromFiles([file], cancellationToken);
        var bound = BoundAvroFile.Bind(LinkedAvroFile.Link(file, symbols, cancellationToken), cancellationToken);
        var field = Assert.Single(Assert.IsType<RecordSchema>(bound.RootSchema).Fields);
        var union = Assert.IsType<UnionTypeSchema>(field.UnderlyingType);
        var logical = Assert.IsType<LogicalSchema>(union.MemberSchemas.Single(member => member is LogicalSchema));

        // Apache preserves the unknown annotation in a wrapper with the collection's generic C# name.
        Assert.Equal(logical.UnderlyingSchema.CSharpName, logical.CSharpName);
        Assert.Contains("<", logical.CSharpName.FullName);
        var expectedItem = $"<item>{expectedElement}</item>";
        Assert.Contains(expectedItem, union.Documentation);
        Assert.Contains(expectedItem, field.Remarks);
        var documentation = XElement.Parse($"<summary>{union.Documentation}</summary>");
        Assert.Equal(2, documentation.Element("list")!.Elements("item").Count());

        var input = GeneratorInput.Create([ProjectFile.Schema(schema)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig(LanguageVersion.Preview)
            {
                AvroLibrary = "Apache",
                LanguageFeatures = "CSharp15",
                PreviewFeatures = "Unions",
            });
        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, cancellationToken);
        Assert.Empty(diagnostics);
        var generatedUnion = Assert.Single(driver.GetRunResult().GeneratedTrees,
            tree => tree.FilePath.EndsWith("EnvelopeChoiceUnion.Avro.g.cs", StringComparison.Ordinal));
        Assert.Contains(expectedItem, generatedUnion.ToString());

        var trees = compilation.SyntaxTrees.Select(tree => CSharpSyntaxTree.ParseText(
            tree.GetText(cancellationToken),
            ((CSharpParseOptions)tree.Options).WithDocumentationMode(DocumentationMode.Diagnose),
            tree.FilePath,
            cancellationToken));
        var documentedCompilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(trees);
        Assert.Empty(documentedCompilation.GetDiagnostics(cancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning
                && diagnostic.Id != "CS1591"));
    }
}
