using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Tests;

public sealed class CachingTests
{
    [Fact]
    public void Independent_schema_content_edit_invalidates_the_collected_project_output() =>
        AssertCurrentInvalidationBaseline(IncrementalScenario.IndependentContent());

    [Fact]
    public void Referenced_schema_content_edit_invalidates_the_collected_project_output() =>
        AssertCurrentInvalidationBaseline(IncrementalScenario.ReferencedSchemaContent());

    [Fact]
    public void Schema_identity_edit_invalidates_the_collected_project_output() =>
        AssertCurrentInvalidationBaseline(IncrementalScenario.SchemaIdentity());

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

    private static void AssertCurrentInvalidationBaseline(IncrementalScenario scenario)
    {
        var projectConfig = new ProjectConfig { LanguageVersion = LanguageVersion.CSharp10 };
        projectConfig.ReferenceResolution = scenario.ReferenceResolution;

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

        var trackedSteps = StepTracking.GetTrackedSteps(result);
        var fileOutputs = trackedSteps["AvroFile"].SelectMany(step => step.Outputs).ToArray();
        Assert.Single(fileOutputs, output => output.Reason == IncrementalStepRunReason.Modified);
        Assert.Equal(
            scenario.ExpectedSourceCount - 1,
            fileOutputs.Count(output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));

        Assert.Contains(
            trackedSteps["AvroFiles"].SelectMany(step => step.Outputs),
            output => output.Reason == IncrementalStepRunReason.Modified);
        Assert.Contains(
            trackedSteps["GeneratorOutput"].SelectMany(step => step.Outputs),
            output => output.Reason == IncrementalStepRunReason.Modified);
    }

    private static void AssertSuccessfulGeneration(GeneratorDriverRunResult result, int expectedSourceCount)
    {
        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Equal(expectedSourceCount, result.Results.Single().GeneratedSources.Length);
    }

    private sealed record IncrementalScenario(
        ImmutableArray<ProjectFile> Files,
        int ChangedFileIndex,
        ProjectFile ChangedFile,
        string ReferenceResolution)
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
            ReferenceResolution: "Strict");

        public static IncrementalScenario ReferencedSchemaContent() => new(
            [
                ProjectFile.Schema(Record("Address", "{\"name\": \"LineOne\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("Customer", "{\"name\": \"Address\", \"type\": \"Address\"}")),
                ProjectFile.Schema(Record("Order", "{\"name\": \"Address\", \"type\": \"Address\"}")),
                ProjectFile.Schema(Record("Unrelated", "{\"name\": \"Value\", \"type\": \"string\"}")),
            ],
            ChangedFileIndex: 0,
            ChangedFile: ProjectFile.Schema(Record("Address", "{\"name\": \"LineOne\", \"type\": \"string\"}, {\"name\": \"Revision\", \"type\": \"int\"}")),
            ReferenceResolution: "Deferred");

        public static IncrementalScenario SchemaIdentity() => new(
            [
                ProjectFile.Schema(Record("Original", "{\"name\": \"Value\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("UnrelatedOne", "{\"name\": \"Value\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("UnrelatedTwo", "{\"name\": \"Value\", \"type\": \"string\"}")),
                ProjectFile.Schema(Record("UnrelatedThree", "{\"name\": \"Value\", \"type\": \"string\"}")),
            ],
            ChangedFileIndex: 0,
            ChangedFile: ProjectFile.Schema(Record("Renamed", "{\"name\": \"Value\", \"type\": \"string\"}")),
            ReferenceResolution: "Strict");

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
