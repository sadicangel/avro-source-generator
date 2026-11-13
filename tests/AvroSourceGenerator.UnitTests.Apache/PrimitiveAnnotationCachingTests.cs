using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Compiler.Linking;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CoreSourceText = AvroSourceGenerator.Text.SourceText;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class PrimitiveAnnotationCachingTests
{
    [Fact]
    public void Annotation_edits_refresh_only_the_affected_schema_and_then_cache_the_output()
    {
        const string Initial = "schema R; record R { @custom(42) string Value; string Plain; }";
        const string Independent = "schema S; record S { string Value; }";
        var input = GeneratorInput.Create(
            [ProjectFile.Source(Initial, "r.avdl"), ProjectFile.Source(Independent, "s.avdl")],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = "Apache" });
        var token = TestContext.Current.CancellationToken;
        var driver = input.GeneratorDriver.RunGenerators(input.Compilation, token);
        AssertProperty(driver.GetRunResult(), 42);
        var independent = driver.GetRunResult().GeneratedTrees.Single(tree => tree.FilePath.EndsWith("S.Avro.g.cs", StringComparison.Ordinal)).ToString();

        var changed = new ChangedAdditionalText(input.AdditionalTexts[0].Path, Initial.Replace("42", "43", StringComparison.Ordinal));
        driver = driver.ReplaceAdditionalText(input.AdditionalTexts[0], changed)
            .RunGeneratorsAndUpdateCompilation(input.Compilation, out var compilation, out var diagnostics, token);
        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(token).Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error));
        AssertProperty(driver.GetRunResult(), 43);
        Assert.Equal(independent, driver.GetRunResult().GeneratedTrees.Single(tree => tree.FilePath.EndsWith("S.Avro.g.cs", StringComparison.Ordinal)).ToString());
        var parsed = driver.GetRunResult().Results.Single().TrackedSteps["AvroFile"];
        Assert.Single(parsed.SelectMany(step => step.Outputs), output => output.Reason is IncrementalStepRunReason.Modified);
        Assert.Single(parsed.SelectMany(step => step.Outputs), output => output.Reason is IncrementalStepRunReason.Cached);
        var rendered = driver.GetRunResult().Results.Single().TrackedSteps["RenderedFile"].SelectMany(step => step.Outputs).ToArray();
        Assert.Single(rendered, output => output.Reason is IncrementalStepRunReason.Modified);
        Assert.Single(rendered, output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged);

        driver = driver.RunGenerators(input.Compilation, token);
        AssertProperty(driver.GetRunResult(), 43);
        Assert.All(driver.GetRunResult().Results.Single().TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));

        driver = driver.ReplaceAdditionalText(changed, input.AdditionalTexts[0]).RunGenerators(input.Compilation, token);
        AssertProperty(driver.GetRunResult(), 42);
        var fresh = GeneratorInput.Create([ProjectFile.Source(Independent, "s.avdl")],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = "Apache" });
        Assert.Equal(independent, Assert.Single(fresh.GeneratorDriver.RunGenerators(fresh.Compilation, token).GetRunResult().GeneratedTrees).ToString());
    }

    private static void AssertProperty(GeneratorDriverRunResult result, int expected)
    {
        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.GeneratedTrees.Length);
        foreach (var tree in result.GeneratedTrees)
        {
            var initializer = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
                .OfType<VariableDeclaratorSyntax>().Single(variable => variable.Identifier.ValueText == "_SCHEMA").Initializer!;
            var json = Assert.Single(initializer.DescendantNodes().OfType<LiteralExpressionSyntax>()).Token.ValueText;
            using var serialized = JsonDocument.Parse(Avro.Schema.Parse(json).ToString());
            var fields = serialized.RootElement.GetProperty("fields");
            if (serialized.RootElement.GetProperty("name").GetString() == "R")
            {
                Assert.Equal(expected, fields[0].GetProperty("type").GetProperty("custom").GetInt32());
                Assert.Equal("string", fields[1].GetProperty("type").GetString());
            }
            else
            {
                Assert.Equal("S", serialized.RootElement.GetProperty("name").GetString());
                Assert.Equal("string", fields[0].GetProperty("type").GetString());
            }
        }
    }

    [Fact]
    public void Annotated_primitive_values_and_pipeline_inputs_preserve_equality_and_hash_contracts()
    {
        var token = TestContext.Current.CancellationToken;
        var options = new AvroParseOptions(GenerationTarget.Apache, LanguageFeatures.CSharp12);
        var source = new CoreSourceText("schema.avdl", "schema R; record R { @custom(42) string Value; }");
        var first = AvroFile.Parse(source, options, token);
        var repeated = AvroFile.Parse(source, options, token);
        var changed = AvroFile.Parse(new CoreSourceText("schema.avdl", source.Text.Replace("42", "43", StringComparison.Ordinal)), options, token);
        Assert.True(first.IsValid);
        Assert.True(repeated.IsValid);
        Assert.True(changed.IsValid);
        Assert.Equal(first, repeated);
        Assert.Equal(first.GetHashCode(), repeated.GetHashCode());
        Assert.NotEqual(first, changed);
        var symbols = SymbolTable.FromFiles([first], token);
        var bound = BoundAvroFile.Bind(LinkedAvroFile.Link(first, symbols, token), token);
        var repeatedBound = BoundAvroFile.Bind(LinkedAvroFile.Link(repeated, SymbolTable.FromFiles([repeated], token), token), token);
        var changedBound = BoundAvroFile.Bind(LinkedAvroFile.Link(changed, SymbolTable.FromFiles([changed], token), token), token);
        Assert.Equal(bound, repeatedBound);
        Assert.Equal(bound.GetHashCode(), repeatedBound.GetHashCode());
        Assert.NotEqual(bound, changedBound);
        var primitive = Assert.IsType<PrimitiveSchema>(Assert.Single(Assert.IsType<RecordSchema>(Assert.Single(first.Declarations)).Fields).Type);
        var copy = primitive with { };
        Assert.Equal(primitive, copy);
        Assert.Equal(primitive.GetHashCode(), copy.GetHashCode());
        Assert.NotEqual(AvroSchema.String, primitive);
        Assert.Empty(AvroSchema.String.Properties);
    }

    private sealed class ChangedAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content, Encoding.UTF8);
    }
}
