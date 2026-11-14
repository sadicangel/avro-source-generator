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

    [Theory]
    [InlineData("avdl", "schema R; record R { @custom(1) date value; }")]
    [InlineData("avdl", "schema R; record R { @custom(1) decimal(9, 2) value; }")]
    [InlineData("avpr", """{"protocol":"P","types":[{"type":"fixed","name":"F","size":16,"logicalType":"uuid","custom":1}],"messages":{}}""")]
    public void Logical_custom_property_edits_refresh_generated_schema_then_cache_it(string extension, string text)
    {
        var input = GeneratorInput.Create(
            [new ProjectFile(text, extension, "test." + extension)],
            [MetadataReference.CreateFromFile(typeof(Schema).Assembly.Location)],
            new ProjectConfig { AvroLibrary = "Apache", LanguageVersion = LanguageVersion.CSharp12 });
        var token = TestContext.Current.CancellationToken;
        var driver = input.GeneratorDriver.RunGenerators(input.Compilation, token);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        var first = string.Join("\n", driver.GetRunResult().Results.Single().GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Contains("\"custom\": 1", first, StringComparison.Ordinal);

        var changed = new ChangedAdditionalText(input.AdditionalTexts[0].Path, text.Replace("custom(1)", "custom(2)", StringComparison.Ordinal).Replace("\"custom\":1", "\"custom\":2", StringComparison.Ordinal));
        driver = driver.ReplaceAdditionalText(input.AdditionalTexts[0], changed).RunGenerators(input.Compilation, token);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        var second = string.Join("\n", driver.GetRunResult().Results.Single().GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Contains("\"custom\": 2", second, StringComparison.Ordinal);
        Assert.DoesNotContain("\"custom\": 1", second, StringComparison.Ordinal);

        driver = driver.RunGenerators(input.Compilation, token);
        Assert.All(
            driver.GetRunResult().Results.Single().TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    private sealed class ChangedAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content, Encoding.UTF8);
    }
}
