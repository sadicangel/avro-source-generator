using System.Text;
using Avro;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class LogicalCachingTests
{
    [Fact]
    public void Unknown_logical_type_edit_refreshes_generated_schema_then_caches_it()
    {
        var fixedSchema = TestSchemas.Get("fixed").With("name", "F").With("size", 2);
        var initialSchema = fixedSchema.With("logicalType", "future-a").ToString();
        var changedSchema = fixedSchema.With("logicalType", "future-b").ToString();
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(initialSchema)],
            [MetadataReference.CreateFromFile(typeof(Schema).Assembly.Location)],
            new ProjectConfig
            {
                AvroLibrary = "Apache",
                LanguageVersion = LanguageVersion.CSharp12
            });

        var driver = input.GeneratorDriver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        var first = Assert.Single(driver.GetRunResult().Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains("future-a", first, StringComparison.Ordinal);

        var changed = new ChangedAdditionalText(input.AdditionalTexts[0].Path, changedSchema);
        driver = driver.ReplaceAdditionalText(input.AdditionalTexts[0], changed)
            .RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        var second = Assert.Single(driver.GetRunResult().Results.Single().GeneratedSources).SourceText.ToString();
        Assert.Contains("future-b", second, StringComparison.Ordinal);
        Assert.DoesNotContain("future-a", second, StringComparison.Ordinal);

        driver = driver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        var cached = driver.GetRunResult();
        Assert.All(
            cached.Results.Single().TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    private sealed class ChangedAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content, Encoding.UTF8);
    }
}
