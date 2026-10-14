using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.UnitTests;

public sealed class RuntimeSelectionTests
{
    private const string Schema = """{"type":"record","name":"Example","fields":[]}""";
    private const string ChrMarker = "namespace Chr.Avro.Abstract { public abstract class Schema {} }";
    private const string ApacheMarker = "namespace Avro.Specific { public interface ISpecificRecord {} }";

    [Theory]
    [InlineData("Chr", false)]
    [InlineData("chr", true)]
    [InlineData(" CHR ", false)]
    [InlineData("2", true)]
    [InlineData("Auto", true)]
    [InlineData(null, true)]
    public void Removed_runtime_reports_a_migration_error_and_emits_fallback_models(string? library, bool referencesChr)
    {
        var input = Create(library, referencesChr);
        var result = input.GeneratorDriver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken).GetRunResult();

        AssertRemoved(result);
        var standalone = Create("None");
        var expected = standalone.GeneratorDriver.RunGenerators(standalone.Compilation, TestContext.Current.CancellationToken).GetRunResult();
        Assert.Equal(expected.GeneratedTrees.Single().ToString(), result.GeneratedTrees.Single().ToString());
    }

    [Theory]
    [InlineData("Auto", "AVROSG6002")]
    [InlineData("Chr", "AVROSG6002")]
    [InlineData("Apache", null)]
    [InlineData("None", null)]
    public void Both_runtime_references_require_an_explicit_supported_target(string library, string? diagnosticId)
    {
        var input = Create(library, referencesChr: true, referencesApache: true);
        var result = input.GeneratorDriver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken).GetRunResult();

        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        Assert.Single(result.GeneratedTrees);
        if (diagnosticId is null) Assert.Empty(result.Diagnostics);
        else AssertRemoved(result);
    }

    [Theory]
    [InlineData("Auto", false, "AVROSG6000")]
    [InlineData("Auto", true, null)]
    [InlineData("None", false, null)]
    public void Supported_targets_preserve_generation(string library, bool referencesApache, string? diagnosticId)
    {
        var input = Create(library, referencesApache: referencesApache);
        var result = input.GeneratorDriver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken).GetRunResult();

        Assert.Single(result.GeneratedTrees);
        if (diagnosticId is null) Assert.Empty(result.Diagnostics);
        else Assert.Equal(diagnosticId, Assert.Single(result.Diagnostics).Id);
        Assert.Equal(referencesApache, result.GeneratedTrees.Single().ToString().Contains("ISpecificRecord", StringComparison.Ordinal));
    }

    [Fact]
    public void Changing_removed_configuration_refreshes_diagnostics_without_reparsing()
    {
        var initial = Create("Chr");
        var supported = Create("None");
        var driver = initial.GeneratorDriver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        AssertRemoved(driver.GetRunResult());

        driver = driver.WithUpdatedAnalyzerConfigOptions(supported.OptionsProvider)
            .RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Single(driver.GetRunResult().GeneratedTrees);
        AssertCachedParsing(driver.GetRunResult());

        driver = driver.WithUpdatedAnalyzerConfigOptions(initial.OptionsProvider)
            .RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        AssertRemoved(driver.GetRunResult());
        AssertCachedParsing(driver.GetRunResult());
    }

    [Fact]
    public void Adding_and_removing_chr_refreshes_diagnostics_and_preserves_cached_models()
    {
        var initial = Create("Auto");
        var removed = Create("Auto", referencesChr: true);
        var driver = initial.GeneratorDriver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.Single(driver.GetRunResult().GeneratedTrees);

        driver = driver.RunGenerators(removed.Compilation, TestContext.Current.CancellationToken);
        AssertRemoved(driver.GetRunResult());
        AssertCachedParsing(driver.GetRunResult());

        driver = driver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.Equal("AVROSG6000", Assert.Single(driver.GetRunResult().Diagnostics).Id);
        Assert.Single(driver.GetRunResult().GeneratedTrees);

        driver = driver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["GeneratorConfiguration"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    [Fact]
    public void Migration_inputs_have_distinct_value_semantics()
    {
        var removed = ProjectProperties.FromAnalyzerOptions(Create("Chr").OptionsProvider, TestContext.Current.CancellationToken);
        var automatic = ProjectProperties.FromAnalyzerOptions(Create("Auto").OptionsProvider, TestContext.Current.CancellationToken);
        Assert.NotEqual(removed, automatic);
        Assert.Equal(AvroLibrary.Chr, removed.AvroLibrary);
        var empty = CompilationEnvironment.FromCompilation(Create("Auto").Compilation, TestContext.Current.CancellationToken);
        var chr = CompilationEnvironment.FromCompilation(Create("Auto", referencesChr: true).Compilation, TestContext.Current.CancellationToken);
        Assert.NotEqual(empty, chr);
        var reference = Assert.Single(chr.AvroLibraries);
        Assert.Equal("Chr.Avro", reference.PackageName);
        Assert.Equal(AvroLibrary.Chr, reference.ToAvroLibrary());
    }

    [Fact]
    public void Runtime_reference_order_preserves_value_semantics()
    {
        var input = Create("Auto", referencesChr: true, referencesApache: true);
        var environment = CompilationEnvironment.FromCompilation(input.Compilation, TestContext.Current.CancellationToken);
        var reversed = new CompilationEnvironment([.. environment.AvroLibraries.Reverse()], environment.LanguageVersion);

        Assert.Equal(environment, reversed);
        Assert.Equal(environment.GetHashCode(), reversed.GetHashCode());
    }

    private static GeneratorInput Create(string? library, bool referencesChr = false, bool referencesApache = false)
    {
        var config = new ProjectConfig();
        if (library is not null) config.AvroLibrary = library;
        var markers = (referencesChr ? ChrMarker : string.Empty) + (referencesApache ? ApacheMarker : string.Empty);
        return GeneratorInput.Create([ProjectFile.Schema(Schema), ProjectFile.CSharp(markers)], [], config);
    }

    private static void AssertRemoved(GeneratorDriverRunResult result)
    {
        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("AVROSG6002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("0.8.0", diagnostic.GetMessage());
        Assert.Contains("Apache", diagnostic.GetMessage());
        Assert.Contains("None", diagnostic.GetMessage());
        Assert.Contains("0.7.x", diagnostic.GetMessage());
        Assert.DoesNotContain("ISpecificRecord", Assert.Single(result.GeneratedTrees).ToString());
    }

    private static void AssertCachedParsing(GeneratorDriverRunResult result) =>
        Assert.All(StepTracking.GetTrackedSteps(result)["AvroFile"].SelectMany(step => step.Outputs),
            output => Assert.True(output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));
}
