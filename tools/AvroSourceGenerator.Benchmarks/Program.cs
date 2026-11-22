using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Benchmarks;

internal static class Program
{
    public static int Main(string[] args)
    {
        GeneratorLocations.EnsureAssembliesExist();

        if (args.Contains("--smoke", StringComparer.OrdinalIgnoreCase))
        {
            return SmokeTest.Run();
        }

        Console.WriteLine($"Published baseline: {GeneratorLocations.BaselineVersion} ({GeneratorLocations.BaselineAssemblyPath})");
        Console.WriteLine("Current: local project output");

        var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        return summaries.All(summary => !summary.HasCriticalValidationErrors && summary.Reports.All(report => report.Success)) ? 0 : 1;
    }
}

[MemoryDiagnoser]
public class FullGenerationBenchmarks
{
    private BenchmarkEnvironment _environment = null!;

    [Params(BenchmarkScenario.DefaultSchemaCount)]
    public int SchemaCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _environment = BenchmarkEnvironment.Create(SchemaCount, prepareIncrementalRuns: false);
        _environment.ValidateFullRuns();
    }

    [Benchmark(Baseline = true)] public GeneratorDriverRunResult Baseline() => _environment.RunFullBaseline();

    [Benchmark] public GeneratorDriverRunResult CurrentTip() => _environment.RunFullCurrent();

    [GlobalCleanup] public void Cleanup() => _environment.Dispose();
}

[MemoryDiagnoser]
public class IncrementalGenerationBenchmarks
{
    private BenchmarkEnvironment _environment = null!;

    [Params(BenchmarkScenario.DefaultSchemaCount)]
    public int SchemaCount { get; set; }

    [Params(
        IncrementalChangeScenario.IndependentContent,
        IncrementalChangeScenario.SchemaIdentity)]
    public IncrementalChangeScenario ChangeScenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _environment = BenchmarkEnvironment.Create(SchemaCount, prepareIncrementalRuns: true, ChangeScenario);
        _environment.ValidateIncrementalRuns();
    }

    [Benchmark(Baseline = true)] public GeneratorDriverRunResult Baseline() => _environment.RunIncrementalBaseline();

    [Benchmark] public GeneratorDriverRunResult CurrentTip() => _environment.RunIncrementalCurrent();

    [GlobalCleanup] public void Cleanup() => _environment.Dispose();
}

[MemoryDiagnoser]
public class ReferencedIncrementalGenerationBenchmarks
{
    private BenchmarkEnvironment _environment = null!;

    [Params(BenchmarkScenario.DefaultSchemaCount)]
    public int SchemaCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _environment = BenchmarkEnvironment.Create(
            SchemaCount,
            prepareIncrementalRuns: true,
            IncrementalChangeScenario.ReferencedSchemaContent);
        _environment.ValidateIncrementalRuns();
    }

    [Benchmark(Baseline = true)] public GeneratorDriverRunResult Baseline() => _environment.RunIncrementalBaseline();

    [Benchmark] public GeneratorDriverRunResult CurrentTip() => _environment.RunIncrementalCurrent();

    [GlobalCleanup] public void Cleanup() => _environment.Dispose();
}

[MemoryDiagnoser]
public class UnionCollectionGenerationBenchmarks
{
    private BenchmarkEnvironment _environment = null!;

    [Params(BenchmarkScenario.DefaultSchemaCount)]
    public int SchemaCount { get; set; }

    [ParamsAllValues]
    public UnionCollectionWorkload Workload { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _environment = BenchmarkEnvironment.CreateUnionCollections(SchemaCount, Workload);
        Validate(_environment, SchemaCount, Workload);
    }

    [Benchmark(Baseline = true)] public GeneratorDriverRunResult Baseline() => _environment.RunFullBaseline();

    [Benchmark] public GeneratorDriverRunResult CurrentTip() => _environment.RunFullCurrent();

    [GlobalCleanup] public void Cleanup() => _environment.Dispose();

    internal static void Validate(BenchmarkEnvironment environment, int schemaCount, UnionCollectionWorkload workload)
    {
        const int recordOnly = 1;
        const int recordWithFieldUnions = 3;
        const int recordWithCollectionUnions = 7;
        var baselinePerRecord = workload == UnionCollectionWorkload.CollectionUnionsOptOut ? recordOnly : recordWithFieldUnions;
        var currentPerRecord = workload == UnionCollectionWorkload.NativeCollectionUnions ? recordWithCollectionUnions : baselinePerRecord;
        environment.ValidateFullRuns(schemaCount * baselinePerRecord, schemaCount * currentPerRecord);
        Console.WriteLine($"{workload}: {schemaCount * baselinePerRecord} baseline sources, {schemaCount * currentPerRecord} current sources.");
    }
}

internal static class SmokeTest
{
    private const int SchemaCount = 8;

    public static int Run()
    {
        using var fullEnvironment = BenchmarkEnvironment.Create(SchemaCount, prepareIncrementalRuns: false);
        var full = fullEnvironment.ValidateFullRuns();

        Console.WriteLine($"Validated full generation for {GeneratorLocations.BaselineVersion} and the current tip with {SchemaCount} schemas.");
        Console.WriteLine($"Full generation: {full.GeneratedSourceCount} sources per version.");

        foreach (var changeScenario in Enum.GetValues<IncrementalChangeScenario>())
        {
            using var incrementalEnvironment = BenchmarkEnvironment.Create(SchemaCount, prepareIncrementalRuns: true, changeScenario);
            var incremental = incrementalEnvironment.ValidateIncrementalRuns();
            Console.WriteLine($"{changeScenario}: {incremental.GeneratedSourceCount} sources per version.");
        }

        foreach (var workload in Enum.GetValues<UnionCollectionWorkload>())
        {
            using var environment = BenchmarkEnvironment.CreateUnionCollections(SchemaCount, workload);
            UnionCollectionGenerationBenchmarks.Validate(environment, SchemaCount, workload);
        }

        Console.WriteLine("No generator errors were reported.");
        return 0;
    }
}
