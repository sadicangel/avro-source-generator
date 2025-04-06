using System.Collections.Immutable;
using AvroSourceGenerator.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Tests;

public sealed class CachingTests
{
    [Fact]
    public void All_outputs_are_reused_when_input_is_unchanged()
    {
        var schema = """
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

        var source = """
        using AvroSourceGenerator;

        [Avro]
        internal partial class User;
        """;

        var projectConfig = ProjectConfig.Default with { LanguageVersion = LanguageVersion.CSharp10 };

        var (_, _, compilation, generatorDriver) =
            GeneratorSetup.Create([source], [schema], projectConfig);

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
        var trackedSteps1 = TestHelper.GetTrackedSteps(result1);
        var trackedSteps2 = TestHelper.GetTrackedSteps(result2);

        Assert.Equal(trackedSteps1.Count, trackedSteps2.Count);
        Assert.All(trackedSteps1.Keys, key => Assert.True(trackedSteps2.ContainsKey(key)));
        Assert.All(trackedSteps2.Keys, key => Assert.True(trackedSteps1.ContainsKey(key)));

        Assert.All(trackedSteps1.Keys, key => AssertStepsAreEqual(trackedSteps1[key], trackedSteps2[key]));
    }

    private static void AssertStepsAreEqual(ImmutableArray<IncrementalGeneratorRunStep> steps1, ImmutableArray<IncrementalGeneratorRunStep> steps2)
    {
        Assert.Equal(steps1.Length, steps2.Length);
        for (var i = 0; i < steps1.Length; i++)
        {
            // Same output value for all runs.
            Assert.Equal(steps1[i].Outputs.Select(x => x.Value), steps2[i].Outputs.Select(x => x.Value));

            // Second output reason must cached or unchanged.
            Assert.All(steps2[i].Outputs.Select(x => x.Reason),
                reason => Assert.True(reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));
        }
    }
}
