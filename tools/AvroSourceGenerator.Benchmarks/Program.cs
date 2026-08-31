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

        Console.WriteLine($"Last GA: {GeneratorLocations.LastGaVersion}");
        Console.WriteLine("Current: local project output");

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        return 0;
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

    [Benchmark(Baseline = true)]
    public GeneratorDriverRunResult LastGa() => _environment.RunFullLastGa();

    [Benchmark]
    public GeneratorDriverRunResult CurrentTip() => _environment.RunFullCurrent();

    [GlobalCleanup]
    public void Cleanup() => _environment.Dispose();
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

    [Benchmark(Baseline = true)]
    public GeneratorDriverRunResult LastGa() => _environment.RunIncrementalLastGa();

    [Benchmark]
    public GeneratorDriverRunResult CurrentTip() => _environment.RunIncrementalCurrent();

    [GlobalCleanup]
    public void Cleanup() => _environment.Dispose();
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
        _environment.ValidateCurrentIncrementalRun();
    }

    [Benchmark]
    public GeneratorDriverRunResult CurrentTip() => _environment.RunIncrementalCurrent();

    [GlobalCleanup]
    public void Cleanup() => _environment.Dispose();
}

internal static class SmokeTest
{
    private const int SchemaCount = 8;

    public static int Run()
    {
        using var fullEnvironment = BenchmarkEnvironment.Create(SchemaCount, prepareIncrementalRuns: false);
        var full = fullEnvironment.ValidateFullRuns();

        Console.WriteLine($"Validated full generation for GA {GeneratorLocations.LastGaVersion} and the current tip with {SchemaCount} schemas.");
        Console.WriteLine($"Full generation: {full.GeneratedSourceCount} sources per version.");

        foreach (var changeScenario in Enum.GetValues<IncrementalChangeScenario>())
        {
            using var incrementalEnvironment = BenchmarkEnvironment.Create(SchemaCount, prepareIncrementalRuns: true, changeScenario);
            var incremental = changeScenario == IncrementalChangeScenario.ReferencedSchemaContent
                ? incrementalEnvironment.ValidateCurrentIncrementalRun()
                : incrementalEnvironment.ValidateIncrementalRuns();
            var comparison = changeScenario == IncrementalChangeScenario.ReferencedSchemaContent
                ? "current tip only"
                : "per version";
            Console.WriteLine($"{changeScenario}: {incremental.GeneratedSourceCount} sources ({comparison}).");
        }

        Console.WriteLine("No generator errors were reported.");
        return 0;
    }
}
