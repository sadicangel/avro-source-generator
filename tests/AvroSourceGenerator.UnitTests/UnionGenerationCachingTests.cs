using AvroSourceGenerator.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests;

public sealed class UnionGenerationCachingTests
{
    [Theory]
    [InlineData(LanguageFeatures.CSharp14)]
    [InlineData(LanguageFeatures.CSharp15)]
    public void Adding_null_refreshes_field_documentation_even_when_generated_members_are_unchanged(LanguageFeatures features)
    {
        var config = new ProjectConfig(features.HasUnions ? LanguageVersion.Preview : LanguageVersion.CSharp14)
        {
            AvroLibrary = "None",
            LanguageFeatures = features.ToString(),
            PreviewFeatures = features.HasUnions ? "Unions" : "None",
        };
        var initial = GeneratorInput.Create([ProjectFile.Schema(VariantGenerationAssertions.Schema("record", "record"), "envelope.avsc")], [], config);
        var changed = GeneratorInput.Create([ProjectFile.Schema(VariantGenerationAssertions.Schema("record", "record", nullable: true), "envelope.avsc")], [], config);
        var driver = initial.GeneratorDriver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("<see langword=\"null\"/>", string.Join("\n", driver.GetRunResult().GeneratedTrees));

        driver = driver.ReplaceAdditionalText(initial.AdditionalTexts[0], changed.AdditionalTexts[0])
            .RunGeneratorsAndUpdateCompilation(initial.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
        var source = string.Join("\n", driver.GetRunResult().GeneratedTrees);
        Assert.Contains("<see langword=\"null\"/>", source);
        Assert.Contains(features.HasUnions ? "EnvelopeChoiceUnion? choice" : "IEnvelopeChoiceVariant? choice", source);

        driver = driver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["RenderedFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    [Theory]
    [InlineData(LanguageFeatures.CSharp14)]
    [InlineData(LanguageFeatures.CSharp15)]
    public void Changing_union_members_refreshes_output_and_then_caches_it(LanguageFeatures features)
    {
        var schema = VariantGenerationAssertions.Schema("record", "record");
        var config = new ProjectConfig(features.HasUnions ? LanguageVersion.Preview : LanguageVersion.CSharp14)
        {
            AvroLibrary = "None",
            LanguageFeatures = features.ToString(),
            PreviewFeatures = features.HasUnions ? "Unions" : "None",
        };
        var initial = GeneratorInput.Create([ProjectFile.Schema(schema, "envelope.avsc")], [], config);
        var changed = GeneratorInput.Create([ProjectFile.Schema(schema.Replace("\"Second\"", "\"Third\""), "envelope.avsc")], [], config);
        var driver = initial.GeneratorDriver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken)
            .ReplaceAdditionalText(initial.AdditionalTexts[0], changed.AdditionalTexts[0])
            .RunGeneratorsAndUpdateCompilation(initial.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Null(compilation.GetTypeByMetadataName("Variants.Second"));
        Assert.NotNull(compilation.GetTypeByMetadataName("Variants.Third"));
        var unionSource = driver.GetRunResult().Results.Single().GeneratedSources.Single(source =>
            source.HintName == (features.HasUnions
                ? "Variants.EnvelopeChoiceUnion.Avro.g.cs"
                : "Variants.IEnvelopeChoiceVariant.Avro.g.cs")).SourceText.ToString();
        Assert.Contains("Third", unionSource);
        Assert.DoesNotContain("Second", unionSource);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["AvroFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));

        driver = driver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["RenderedFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }
}
