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

    [GlobalSetup]
    public void Setup()
    {
        _environment = BenchmarkEnvironment.Create(SchemaCount, prepareIncrementalRuns: true);
        _environment.ValidateIncrementalRuns();
    }

    [Benchmark(Baseline = true)]
    public GeneratorDriverRunResult LastGa() => _environment.RunIncrementalLastGa();

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
        using var environment = BenchmarkEnvironment.Create(SchemaCount, prepareIncrementalRuns: true);

        var full = environment.ValidateFullRuns();
        var incremental = environment.ValidateIncrementalRuns();

        Console.WriteLine($"Validated GA {GeneratorLocations.LastGaVersion} and the current tip with {SchemaCount} schemas.");
        Console.WriteLine($"Full generation:        {full.GeneratedSourceCount} sources per version.");
        Console.WriteLine($"One-file incremental:   {incremental.GeneratedSourceCount} sources per version.");
        Console.WriteLine("No generator errors were reported.");
        return 0;
    }
}
