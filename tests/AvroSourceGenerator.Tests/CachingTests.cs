using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Tests;

public sealed class CachingTests
{
    [Fact]
    public void Independent_schema_content_edit_reuses_unchanged_bound_files() =>
        AssertCurrentInvalidationBaseline(IncrementalScenario.IndependentContent());

    [Fact]
    public void Referenced_schema_content_edit_reuses_unchanged_bound_files() =>
        AssertCurrentInvalidationBaseline(IncrementalScenario.ReferencedSchemaContent());

    [Fact]
    public void Apache_referenced_schema_content_edit_invalidates_transitive_consumers_only() =>
        AssertCurrentInvalidationBaseline(IncrementalScenario.ReferencedSchemaContent(), avroLibrary: "Apache");

    [Fact]
    public void Schema_identity_edit_reuses_unaffected_bound_files() =>
        AssertCurrentInvalidationBaseline(IncrementalScenario.SchemaIdentity());

    [Fact]
    public void Schema_kind_change_relinks_and_rebinds_only_the_declaration_and_its_consumer()
    {
        var files = ImmutableArray.Create(
            ProjectFile.Schema(Record("Shared", "")),
            ProjectFile.Schema(Record("Consumer", "{\"name\": \"shared\", \"type\": \"Shared\"}")),
            ProjectFile.Schema(Record("Unrelated", "")));
        var config = new ProjectConfig { LanguageVersion = LanguageVersion.CSharp10 };
        config.ReferenceResolution = "Deferred";
        var input = GeneratorInput.Create(files, [], config);
        var driver = input.GeneratorDriver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        var changed = new ChangedAdditionalText(
            input.AdditionalTexts[0].Path,
            """
            { "type": "fixed", "namespace": "CachingTests", "name": "Shared", "size": 16 }
            """);

        driver = driver.ReplaceAdditionalText(input.AdditionalTexts[0], changed)
            .RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        var trackedSteps = StepTracking.GetTrackedSteps(driver.GetRunResult());

        Assert.Equal(1, CountModified(trackedSteps, "SymbolTable"));
        Assert.Equal(2, CountModified(trackedSteps, "LinkedAvroFile"));
        Assert.Equal(2, CountModified(trackedSteps, "BoundAvroFile"));
    }

    [Fact]
    public void Reference_resolution_change_rebuilds_the_project_without_reparsing_files()
    {
        var files = ImmutableArray.Create(
            ProjectFile.Schema(Record("Shared", "")),
            ProjectFile.Schema(Record("Consumer", "{\"name\": \"shared\", \"type\": \"Shared\"}")));

        AssertProjectOnlyConfigChange(
            files,
            initial => initial["AvroSourceGeneratorReferenceResolution"] = "Strict",
            changed => changed["AvroSourceGeneratorReferenceResolution"] = "Deferred");
    }

    [Fact]
    public void Duplicate_policy_change_rebuilds_the_project_without_reparsing_files()
    {
        var files = ImmutableArray.Create(
            ProjectFile.Schema(Record("Shared", "")),
            ProjectFile.Schema(Record("Shared", "{\"name\": \"value\", \"type\": \"string\"}")));

        AssertProjectOnlyConfigChange(
            files,
            initial => initial["AvroSourceGeneratorDuplicateResolution"] = "Error",
            changed => changed["AvroSourceGeneratorDuplicateResolution"] = "Ignore");
    }

    [Fact]
    public void All_outputs_are_reused_when_input_is_unchanged()
    {
        const string Schema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "User",
                "fields": [
                    {"name": "FirstName", "type": "string"},
                    {"name": "LastName", "type": "string"},
                    {"name": "Age", "type": "int"},
                    {"name": "IsActive", "type": "boolean"},
                    {"name": "CreatedAt", "type": "long", "logicalType": "timestamp-millis"},
                    {"name": "CreatedBy", "type": "string"},
                    {"name": "IsDeleted", "type": "boolean"}
                ]
            }
            """;

        const string Source = """
            using AvroSourceGenerator;

            [Avro]
            internal partial class User;
            """;

        var projectConfig = new ProjectConfig { LanguageVersion = LanguageVersion.CSharp10 };

        var (compilation, _, generatorDriver, _) =
            GeneratorInput.Create([ProjectFile.CSharp(Source), ProjectFile.Schema(Schema)], [], projectConfig);

        generatorDriver = generatorDriver
            .RunGenerators(compilation, TestContext.Current.CancellationToken);

        var result1 = generatorDriver.GetRunResult();

        generatorDriver = generatorDriver
            .RunGenerators(compilation, TestContext.Current.CancellationToken);

        var result2 = generatorDriver.GetRunResult();

        AssertOutputStepsAreCached(result2);

        AssertRunsAreEqual(result1, result2);
    }

    private static void AssertOutputStepsAreCached(GeneratorDriverRunResult result)
    {
        var outputReasons = result.Results[0]
            .TrackedOutputSteps
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason);

        Assert.All(outputReasons, reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
    }

    private static void AssertRunsAreEqual(GeneratorDriverRunResult result1, GeneratorDriverRunResult result2)
    {
        var trackedSteps1 = StepTracking.GetTrackedSteps(result1);
        var trackedSteps2 = StepTracking.GetTrackedSteps(result2);

        Assert.Equal(trackedSteps1.Count, trackedSteps2.Count);
        Assert.All(trackedSteps1.Keys, key => Assert.True(trackedSteps2.ContainsKey(key)));
        Assert.All(trackedSteps2.Keys, key => Assert.True(trackedSteps1.ContainsKey(key)));

        Assert.All(trackedSteps1.Keys, key => AssertStepsAreEqual(trackedSteps1[key], trackedSteps2[key]));
    }

    private static void AssertStepsAreEqual(
        ImmutableArray<IncrementalGeneratorRunStep> steps1,
        ImmutableArray<IncrementalGeneratorRunStep> steps2)
    {
        Assert.Equal(steps1.Length, steps2.Length);
        for (var i = 0; i < steps1.Length; i++)
        {
            // Same output value for all runs.
            Assert.Equal(steps1[i].Outputs.Select(x => x.Value), steps2[i].Outputs.Select(x => x.Value));

            // Second output reason must be cached or unchanged.
            Assert.All(
                steps2[i].Outputs.Select(x => x.Reason),
                reason => Assert.True(reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));
        }
    }

    private static void AssertCurrentInvalidationBaseline(IncrementalScenario scenario, string? avroLibrary = null)
    {
        var projectConfig = new ProjectConfig { LanguageVersion = LanguageVersion.CSharp10 };
        projectConfig.ReferenceResolution = scenario.ReferenceResolution;
        projectConfig.AvroLibrary = avroLibrary ?? projectConfig.AvroLibrary;

        var input = GeneratorInput.Create(scenario.Files, [], projectConfig);
        var driver = input.GeneratorDriver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        AssertSuccessfulGeneration(driver.GetRunResult(), scenario.ExpectedSourceCount);

        var changedFile = new ChangedAdditionalText(
            input.AdditionalTexts[scenario.ChangedFileIndex].Path,
            scenario.ChangedFile.Content);
        driver = driver.ReplaceAdditionalText(input.AdditionalTexts[scenario.ChangedFileIndex], changedFile)
            .RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();

        AssertSuccessfulGeneration(result, scenario.ExpectedSourceCount);
        AssertSchemaIdentityOutput(result, scenario);

        var trackedSteps = StepTracking.GetTrackedSteps(result);
        var fileOutputs = trackedSteps["AvroFile"].SelectMany(step => step.Outputs).ToArray();
        Assert.Single(fileOutputs, output => output.Reason == IncrementalStepRunReason.Modified);
        Assert.Equal(
            scenario.ExpectedSourceCount - 1,
            fileOutputs.Count(output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));

        Assert.Equal(scenario.ExpectedSymbolTableInvalidations, CountModified(trackedSteps, "SymbolTable"));

        var linkedFileOutputs = trackedSteps["LinkedAvroFile"].SelectMany(step => step.Outputs).ToArray();
        Assert.Single(linkedFileOutputs, output => output.Reason == IncrementalStepRunReason.Modified);
        Assert.Equal(
            scenario.ExpectedSourceCount - 1,
            linkedFileOutputs.Count(output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));

        var boundFileOutputs = trackedSteps["BoundAvroFile"].SelectMany(step => step.Outputs).ToArray();
        Assert.Single(boundFileOutputs, output => output.Reason == IncrementalStepRunReason.Modified);
        Assert.Equal(
            scenario.ExpectedSourceCount - 1,
            boundFileOutputs.Count(output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));

        Assert.Contains(
            trackedSteps["BoundAvroFiles"].SelectMany(step => step.Outputs),
            output => output.Reason == IncrementalStepRunReason.Modified);
        Assert.Contains(
            trackedSteps["AvroProject"].SelectMany(step => step.Outputs),
            output => output.Reason == IncrementalStepRunReason.Modified);

        AssertRenderFanout(trackedSteps, scenario.ExpectedRenderedFileInvalidations, scenario.ExpectedSourceCount);
    }

    private static void AssertProjectOnlyConfigChange(
        ImmutableArray<ProjectFile> files,
        Action<Dictionary<string, string>> configureInitial,
        Action<Dictionary<string, string>> configureChanged)
    {
        var initialConfig = new ProjectConfig { LanguageVersion = LanguageVersion.CSharp10 };
        configureInitial(initialConfig.GlobalOptions);
        var input = GeneratorInput.Create(files, [], initialConfig);
        var driver = input.GeneratorDriver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken);

        var changedConfig = new ProjectConfig { LanguageVersion = LanguageVersion.CSharp10 };
        configureChanged(changedConfig.GlobalOptions);
        var changedInput = GeneratorInput.Create(files, [], changedConfig);
        driver = driver.WithUpdatedAnalyzerConfigOptions(changedInput.OptionsProvider)
            .RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        var trackedSteps = StepTracking.GetTrackedSteps(driver.GetRunResult());

        Assert.All(
            trackedSteps["AvroFile"].SelectMany(static step => step.Outputs),
            static output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
        Assert.Equal(0, CountModified(trackedSteps, "BoundAvroFile"));
        Assert.Contains(
            trackedSteps["AvroProject"].SelectMany(static step => step.Outputs),
            static output => output.Reason is IncrementalStepRunReason.Modified);
    }

    private static int CountModified(
        ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> trackedSteps,
        string stepName) =>
        trackedSteps.TryGetValue(stepName, out var steps)
            ? steps
                .SelectMany(static step => step.Outputs)
                .Count(static output => output.Reason is IncrementalStepRunReason.Modified)
            : 0;

    private static string Record(string name, string fields) => $$"""
        {
          "type": "record",
          "namespace": "CachingTests",
          "name": "{{name}}",
          "fields": [{{fields}}]
        }
        """;

    private static void AssertRenderFanout(
        ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> trackedSteps,
        int expectedModifiedFiles,
        int expectedFileCount)
    {
        AssertStepHasExpectedFanout("RenderableAvroFile");
        AssertStepHasExpectedFanout("RenderedFile");

        void AssertStepHasExpectedFanout(string stepName)
        {
            var outputs = trackedSteps[stepName].SelectMany(step => step.Outputs).ToArray();
            Assert.Equal(expectedModifiedFiles, outputs.Count(output => output.Reason == IncrementalStepRunReason.Modified));
            Assert.Equal(
                expectedFileCount - expectedModifiedFiles,
                outputs.Count(output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));
        }
    }

    private static void AssertSuccessfulGeneration(GeneratorDriverRunResult result, int expectedSourceCount)
    {
        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Equal(expectedSourceCount, result.Results.Single().GeneratedSources.Length);
    }

    private static void AssertSchemaIdentityOutput(GeneratorDriverRunResult result, IncrementalScenario scenario)
    {
        if (scenario.RemovedHintName is not { } removedHintName || scenario.AddedHintName is not { } addedHintName)
            return;

        var generatedSources = result.Results.Single().GeneratedSources;
        Assert.DoesNotContain(generatedSources, source => source.HintName == removedHintName);
        Assert.Contains(generatedSources, source => source.HintName == addedHintName);
    }

    private sealed record IncrementalScenario(
        ImmutableArray<ProjectFile> Files,
        int ChangedFileIndex,
        ProjectFile ChangedFile,
        string ReferenceResolution,
        int ExpectedSymbolTableInvalidations,
        int ExpectedRenderedFileInvalidations,
        string? RemovedHintName = null,
        string? AddedHintName = null)
    {
        public int ExpectedSourceCount => Files.Length;

        public static IncrementalScenario IndependentContent() => new(
            [
                ProjectFile.Schema(Record("Independent", "{\"name\": \"Value\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("UnrelatedOne", "{\"name\": \"Value\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("UnrelatedTwo", "{\"name\": \"Value\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("UnrelatedThree", "{\"name\": \"Value\", \"type\": \"string\"}")),
            ],
            ChangedFileIndex: 0,
            ChangedFile: ProjectFile.Schema(Record("Independent", "{\"name\": \"Value\", \"type\": \"string\"}, {\"name\": \"Revision\", \"type\": \"int\"}")),
            ReferenceResolution: "Strict",
            ExpectedSymbolTableInvalidations: 0,
            ExpectedRenderedFileInvalidations: 1);

        public static IncrementalScenario ReferencedSchemaContent() => new(
            [
                ProjectFile.Schema(Record("Address", "{\"name\": \"LineOne\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("Customer", "{\"name\": \"Address\", \"type\": \"Address\"}")),
                ProjectFile.Schema(Record("Order", "{\"name\": \"Customer\", \"type\": \"Customer\"}")),
                ProjectFile.Schema(Record("Unrelated", "{\"name\": \"Value\", \"type\": \"string\"}")),
            ],
            ChangedFileIndex: 0,
            ChangedFile: ProjectFile.Schema(Record("Address", "{\"name\": \"LineOne\", \"type\": \"string\"}, {\"name\": \"Revision\", \"type\": \"int\"}")),
            ReferenceResolution: "Deferred",
            ExpectedSymbolTableInvalidations: 0,
            ExpectedRenderedFileInvalidations: 3);

        public static IncrementalScenario SchemaIdentity() => new(
            [
                ProjectFile.Schema(Record("Original", "{\"name\": \"Value\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("UnrelatedOne", "{\"name\": \"Value\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("UnrelatedTwo", "{\"name\": \"Value\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("UnrelatedThree", "{\"name\": \"Value\", \"type\": \"string\"}")),
            ],
            ChangedFileIndex: 0,
            ChangedFile: ProjectFile.Schema(Record("Renamed", "{\"name\": \"Value\", \"type\": \"string\"}")),
            ReferenceResolution: "Strict",
            ExpectedSymbolTableInvalidations: 1,
            ExpectedRenderedFileInvalidations: 1,
            RemovedHintName: "CachingTests.Original.Avro.g.cs",
            AddedHintName: "CachingTests.Renamed.Avro.g.cs");

        private static string Record(string name, string fields) => $$"""
            {
              "type": "record",
              "namespace": "CachingTests",
              "name": "{{name}}",
              "fields": [{{fields}}]
            }
            """;
    }

    private sealed class ChangedAdditionalText(string path, string content) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(content);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}
