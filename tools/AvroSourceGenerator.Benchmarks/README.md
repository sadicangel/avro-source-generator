# AvroSourceGenerator benchmarks

This project compares the current local source-generator project with the latest stable NuGet release. The GA baseline is intentionally pinned in `AvroSourceGenerator.Benchmarks.csproj` so a report remains reproducible; update the `LastGaVersion` property when a new stable release is published. Preview releases are never selected implicitly.

The default workload contains 250 independent Avro record schemas with 36 fields each (9,000 fields total). The fields exercise primitives, nullable unions, arrays, maps, timestamps, decimals, defaults, and Apache-specific output. Both versions use C# 12 output so init-only properties, required properties, records, nullable annotations, raw strings, and unsafe accessors are enabled.

Two operations are measured:

- `FullGenerationBenchmarks` creates a fresh Roslyn generator driver and generates every schema.
- `IncrementalGenerationBenchmarks` starts from a primed driver, changes one schema by adding a field, and runs the incremental pipeline again.

Schema construction, generator assembly loading, initial incremental generation, and result validation happen outside the measured operations. The benchmark measures generator-driver execution and source production, but not NuGet restore, assembly loading, or compilation of generated C#.

Run the validation-only smoke test first:

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --smoke
```

Run all benchmarks:

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release --
```

For a faster, lower-confidence local check, add `--job short` after the final `--`.

Run only one scenario:

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter "*FullGenerationBenchmarks*"
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter "*IncrementalGenerationBenchmarks*"
```

BenchmarkDotNet reports `LastGa` as the baseline. For `CurrentTip`, a time or allocation ratio above `1.00` is a regression and a ratio below `1.00` is an improvement. Focus on ratios larger than the reported noise rather than small single-run differences, and compare results on the same idle machine and runtime.

No CI or pipeline integration is included. Benchmark results are written to the ignored `BenchmarkDotNet.Artifacts` directory.
