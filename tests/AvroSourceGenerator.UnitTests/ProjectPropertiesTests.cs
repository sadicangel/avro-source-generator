using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests;

public sealed class ProjectPropertiesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public void Missing_empty_and_invalid_options_receive_concrete_defaults(string? value)
    {
        var config = new ProjectConfig();
        if (value is not null)
        {
            config.AvroLibrary = value;
            config.LanguageFeatures = value;
            config.AccessModifier = value;
            config.RecordDeclaration = value;
            config.ReferenceResolution = value;
            config.DuplicateResolution = value;
            config.PreviewFeatures = value;
        }

        Assert.Equal(new ProjectProperties(
            AvroLibrary.Auto, LanguageFeatures.All, AccessModifier.Public, "record",
            ReferenceResolution.Strict, DuplicateResolution.Error, PreviewFeatures.None), Read(config));
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp7_3, LanguageFeatures.CSharp7_3)]
    [InlineData(LanguageVersion.CSharp8, LanguageFeatures.CSharp8)]
    [InlineData(LanguageVersion.CSharp9, LanguageFeatures.CSharp9)]
    [InlineData(LanguageVersion.CSharp10, LanguageFeatures.CSharp10)]
    [InlineData(LanguageVersion.CSharp11, LanguageFeatures.CSharp11)]
    [InlineData(LanguageVersion.CSharp12, LanguageFeatures.CSharp12)]
    [InlineData(LanguageVersion.CSharp13, LanguageFeatures.CSharp13)]
    [InlineData(LanguageVersion.CSharp14, LanguageFeatures.CSharp14)]
    [InlineData((LanguageVersion)1500, LanguageFeatures.CSharp15)]
    [InlineData(LanguageVersion.Preview, LanguageFeatures.CSharp15)]
    public void All_features_resolve_against_the_project_language_version(
        LanguageVersion version, LanguageFeatures features)
    {
        var properties = Read(new ProjectConfig() { AvroLibrary = "Chr", PreviewFeatures = "Unions" });
        var configuration = GeneratorConfiguration.Resolve((properties, new CompilationEnvironment([], version)), TestContext.Current.CancellationToken);

        Assert.Equal(LanguageFeatures.All, properties.LanguageFeatures);
        Assert.Equal(features, configuration.LanguageFeatures);
    }

    [Theory]
    [InlineData("None", "class")]
    [InlineData("CSharp8", "class")]
    [InlineData("CSharp15", "record")]
    public void Record_default_is_resolved_against_the_requested_language_features(string features, string declaration)
    {
        var properties = Read(new ProjectConfig() { LanguageFeatures = features });
        var configuration = GeneratorConfiguration.Resolve((properties, new CompilationEnvironment([], LanguageVersion.Preview)), TestContext.Current.CancellationToken);

        Assert.Equal("record", properties.RecordDeclaration);
        Assert.Equal(declaration == "record", configuration.LanguageFeatures.HasRecords);
    }

    [Fact]
    public void Explicit_defaults_and_omitted_options_have_equal_properties_and_hashes()
    {
        var omitted = Read(new ProjectConfig());
        var explicitDefaults = Read(new ProjectConfig()
        {
            AvroLibrary = "Auto",
            LanguageFeatures = "All",
            AccessModifier = "Public",
            RecordDeclaration = "record",
            ReferenceResolution = "Strict",
            DuplicateResolution = "Error",
            PreviewFeatures = "None",
        });

        Assert.Equal(omitted, explicitDefaults);
        Assert.Equal(omitted.GetHashCode(), explicitDefaults.GetHashCode());
    }

    [Fact]
    public void Changing_language_version_refreshes_configuration_without_reparsing_properties()
    {
        ProjectFile[] files =
        [
            ProjectFile.Schema(VariantGenerationAssertions.Schema("record", "record")),
            ProjectFile.CSharp("namespace Consumer { class Marker {} }"),
        ];
        var initial = GeneratorInput.Create([.. files], [], new ProjectConfig(LanguageVersion.CSharp8) { AvroLibrary = "None" });
        var changed = GeneratorInput.Create([.. files], [], new ProjectConfig() { AvroLibrary = "None" });
        var driver = initial.GeneratorDriver.RunGenerators(initial.Compilation, TestContext.Current.CancellationToken);
        Assert.Contains("partial class Envelope", string.Join("\n", driver.GetRunResult().GeneratedTrees));

        driver = driver.WithUpdatedParseOptions(new CSharpParseOptions(LanguageVersion.Preview))
            .RunGeneratorsAndUpdateCompilation(changed.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains("partial record Envelope", string.Join("\n", driver.GetRunResult().GeneratedTrees));
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["ProjectProperties"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["GeneratorConfiguration"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));

        driver = driver.RunGenerators(changed.Compilation, TestContext.Current.CancellationToken);
        Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["RenderedFile"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp8, "CSharp15", "record", LanguageFeatures.CSharp8)]
    [InlineData(LanguageVersion.CSharp14, "All", "class", LanguageFeatures.CSharp14 & ~LanguageFeatures.Records)]
    [InlineData(LanguageVersion.Preview, "CSharp8", "class", LanguageFeatures.CSharp8)]
    [InlineData(LanguageVersion.Preview, "None", "record", LanguageFeatures.None)]
    [InlineData(LanguageVersion.CSharp8, "All", "record", LanguageFeatures.CSharp8)]
    public void Record_overrides_and_requested_features_respect_the_compiler_version(
        LanguageVersion version, string features, string declaration, LanguageFeatures expected)
    {
        var properties = Read(new ProjectConfig()
        {
            AvroLibrary = "Chr",
            LanguageFeatures = features,
            RecordDeclaration = declaration,
        });
        var configuration = GeneratorConfiguration.Resolve((properties, new CompilationEnvironment([], version)), TestContext.Current.CancellationToken);

        Assert.Equal(expected, configuration.LanguageFeatures);
    }

    private static ProjectProperties Read(ProjectConfig config) =>
        ProjectProperties.FromAnalyzerOptions(GeneratorInput.Create([], [], config).OptionsProvider, TestContext.Current.CancellationToken);
}
