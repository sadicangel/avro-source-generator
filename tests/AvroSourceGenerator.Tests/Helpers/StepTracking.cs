using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Helpers;

internal static class StepTracking
{
    public static readonly ImmutableArray<string> TrackingNames = [.. typeof(AvroSourceGenerator)
        .Assembly
        .GetType("AvroSourceGenerator.Parsing.TrackingNames", throwOnError: true)!
        .GetFields()
        .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
        .Select(x => (string?)x.GetRawConstantValue()!)
        .Where(x => !string.IsNullOrEmpty(x))];

    public static ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(GeneratorDriverRunResult result) =>
        result.Results[0].TrackedSteps.Where(x => TrackingNames.Contains(x.Key)).ToImmutableDictionary();
}
