# AvroSourceGenerator benchmarks

The generator benchmarks compare the current local source-generator project with the latest stable NuGet release. The GA baseline is pinned in `AvroSourceGenerator.Benchmarks.csproj` to keep reports reproducible. Update the `LastGaVersion` property when a new stable release is published. Preview releases are selected only when explicitly configured.

The default workload contains 250 independent Avro record schemas with 36 fields each (9,000 fields total). The fields exercise primitives, nullable unions, arrays, maps, timestamps, decimals, defaults, and Apache-specific output. Both versions use C# 12 output so init-only properties, required properties, records, nullable annotations, raw strings, and unsafe accessors are enabled.

Two operations are measured across three benchmark classes:

- `FullGenerationBenchmarks` creates a fresh Roslyn generator driver and generates every schema.
- `IncrementalGenerationBenchmarks` compares GA and the current tip after an independent content or schema identity edit.
- `ReferencedIncrementalGenerationBenchmarks` measures the current tip after a referenced schema changes. It is current-tip-only because 0.6.0 does not support Deferred cross-file references.

The incremental scenarios are:

- `IndependentContent` adds a field to an otherwise independent schema.
- `ReferencedSchemaContent` adds a field to a schema referenced by half of the remaining schemas; the other half stay independent. This exercises dependent-consumer fan-out while retaining unrelated schemas for later cache assertions.
- `SchemaIdentity` renames an independent schema, exercising export identity invalidation.

The current pipeline parses schemas, references, and dependency names per file, builds a project symbol table from declaration headers, and projects only the referenced symbols back into each `LinkedAvroFile`. Binding produces a cached `BoundAvroFile`, resolving C# names without replacing schema references. A body-only edit therefore rebinds only that file, while a symbol kind or name change additionally rebinds only files whose linked symbols changed. Collected bound files form a lightweight `AvroProject`; each file is then projected into a `RenderableAvroFile` containing its accepted declarations, the shared project schema lookup, and the owning file revisions in its transitive dependency closure. An independent edit rerenders only that file, while a referenced body edit rerenders the changed file and its direct or transitive consumers; unrelated files remain cached. These scenarios deliberately avoid elapsed-time assertions while recording this behavior.

## Development checkpoint notes

These are temporary notes from the incremental-pipeline work. Both checkpoints used the 250-schema `ShortRun` job. P1 is commit `c676fd6`, where the benchmark guardrails were added; Current is the declaration/link/bind/project/renderable-file pipeline measured on 2026-09-02. Ratios compare each checkpoint with the 0.6.0 result from the same run, which is more useful than comparing absolute values across different sessions.

| Scenario | 0.6.0 | P1 starting point | Current |
|---|---:|---:|---:|
| Full generation | 1.00 | 1.10 time / 0.63 allocation | 1.06 time / 0.59 allocation |
| Independent content edit | 1.00 | 0.88 time / 0.47 allocation | 1.25 time / 1.49 allocation |
| Schema identity edit | 1.00 | 62.97 time / 103.75 allocation | 0.76 time / 1.51 allocation |
| Referenced schema content edit | n/a | 0.64 ms / 0.17 MB | 12.48 ms / 12.44 MB |

Raw measurements retained for context:

- P1 full generation: 0.6.0 114.7 ms and 174.74 MB; P1 125.5 ms and 109.53 MB.
- P1 independent edit: 0.6.0 730.7 µs and 375.16 KB; P1 631.3 µs and 176.54 KB.
- P1 schema identity edit: 0.6.0 3.377 ms and 1.04 MB; P1 211.4 ms and 107.81 MB. This was the original whole-project invalidation problem.
- Current full generation: 0.6.0 203.3 ms and 174.84 MB; Current 211.2 ms and 103.72 MB.
- Current independent edit: 0.6.0 3.278 ms and 1.05 MB; Current 3.759 ms and 1.55 MB.
- Current schema identity edit: 0.6.0 3.165 ms and 1.04 MB; Current 2.388 ms and 1.57 MB.
- The referenced-edit numbers are not directly comparable: P1 did not yet propagate the changed schema body through real dependency closures, while Current intentionally rerenders the changed owner and all transitive consumers.

The short runs have only three measured iterations and wide confidence intervals. They are useful for documenting the large improvement in schema-identity invalidation, but not for treating small differences as regressions.

Schema construction, generator assembly loading, initial incremental generation, and result validation happen outside the measured operations. The benchmark measures generator-driver execution and source production, but not NuGet restore, assembly loading, or compilation of generated C#.

Run the validation-only smoke test first:

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --smoke
```

Run all benchmarks from the repository root. Quote the wildcard in PowerShell so BenchmarkDotNet selects every class without prompting.

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter '*'
```

The benchmark project pins the NuGet baseline to `LastGaVersion` in its `.csproj` and loads the current generator from the local Release build, including uncommitted changes. The run prints both versions before measuring. It validates generated source counts and checks for generator errors during setup. Allow several minutes for the full run. From the repository root, BenchmarkDotNet writes its reports under `BenchmarkDotNet.Artifacts/results/`.

For a faster, lower-confidence local check, add `--job short` after `--filter '*'`.

Run only one scenario:

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter "*FullGenerationBenchmarks*"
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter "*IncrementalGenerationBenchmarks*"
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter "*ReferencedIncrementalGenerationBenchmarks*"
```

BenchmarkDotNet reports `LastGa` as the baseline. For `CurrentTip`, a time or allocation ratio above `1.00` is a regression and a ratio below `1.00` is an improvement. Focus on ratios larger than the reported noise rather than small single-run differences, and compare results on the same idle machine and runtime.

## Local reference binding

`LocalReferenceBindingBenchmarks` measures the local Core build with 10 or 1,000 enum declarations. Each case binds an already parsed and linked protocol. The zero-reference case measures binding without building a local name index. The single-reference case targets the first enum, so comparing it across revisions shows the net cost of constructing the index against a minimal scan. The 2,000-reference case targets the last enum to exercise repeated lookups.

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter '*LocalReferenceBindingBenchmarks*' --job short --inProcess
```

An in-process short run on Windows 11 with SDK 11.0.100 compared the indexed binder with commit `8da4c86`.

| Enums | Local references | Mean before (µs) | Mean indexed (µs) | Allocated before (KiB) | Allocated indexed (KiB) |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 10 | 0 | 0.393 | 0.390 | 1.21 | 1.22 |
| 10 | 1 | 0.432 | 0.738 | 1.38 | 2.08 |
| 10 | 2,000 | 168.45 | 45.47 | 329.34 | 2.08 |
| 1,000 | 0 | 30.89 | 32.00 | 107.80 | 107.80 |
| 1,000 | 1 | 37.55 | 64.33 | 107.96 | 155.33 |
| 1,000 | 2,000 | 11,156.68 | 111.54 | 435.98 | 155.33 |

In the single-reference case, the net index setup cost was about 0.31 µs and 0.70 KiB for 10 enums, and 27 µs and 47 KiB for 1,000 enums. These differences include the change in lookup overhead.

## Import analysis

`ImportAnalysisBenchmarks` measures `AvroCompilation.Create` after parsing, linking, and binding. The direct, chain, and diamond cases vary the number of distinct names referenced from one protocol. The chain contains 32 intermediate files, and the diamond includes an unused branch. The distinct-owner case uses Deferred resolution with 512 independent consumer/owner pairs. The other cases use Strict resolution, including the unresolved-name case with a cycle, a missing import, and a missing-reference diagnostic.

`ImportAnalysisIncrementalBenchmarks` runs the generator against the 256-reference diamond after priming the driver. It applies either an independent IDL edit in Strict mode or a referenced protocol edit in Deferred mode. Setup checks the diagnostics for Core compilation or validates the generated source count and absence of errors for generator runs.

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter '*ImportAnalysis*' --job short --inProcess
```

The measurements used BenchmarkDotNet 0.14.0 on Windows 11 with SDK 11.0.100 and runtime 11.0.0. Both runs used the same benchmark harness. The baseline was the compiler and generator at `db65bc9`. The short job uses three measured iterations, so small timing differences are inconclusive. [GitHub issue #200](https://github.com/sadicangel/avro-source-generator/issues/200)

| Workload | Mean before (µs) | Mean candidate (µs) | Allocated before (KiB) | Allocated candidate (KiB) |
| --- | ---: | ---: | ---: | ---: |
| Standalone record | 1.043 | 1.154 | 3.63 | 3.63 |
| Direct import, 1 reference | 2.986 | 3.196 | 6.84 | 6.76 |
| Direct import, 256 references | 210.77 | 184.63 | 249.06 | 181.90 |
| Chain, 1 reference | 66.32 | 62.86 | 114.12 | 108.48 |
| Chain, 256 references | 743.85 | 265.16 | 625.83 | 302.63 |
| Diamond, 1 reference | 8.03 | 7.18 | 14.30 | 14.23 |
| Diamond, 256 references | 265.64 | 178.69 | 297.88 | 188.79 |
| 512 distinct owners | 1,712.27 | 1,549.70 | 2,923.60 | 2,763.83 |
| Unresolved names | 201.35 | 118.56 | 179.92 | 59.87 |
| Independent incremental edit | 800.8 | 783.9 | 583.23 | 470.38 |
| Referenced incremental edit | 16,652.0 | 18,415.8 | 14,218.53 | 14,106.03 |

The 256-reference chain used 64% less time and allocated 52% less. The unresolved case allocated 67% less. The distinct-owner workload still incurs 512 KiB of boolean reachability data across its 1,024 files. The independent edit allocated about 19% less. The referenced edit had a higher timing mean and wide confidence intervals, with 0.8% lower allocation across the full generation pass.

No CI or pipeline integration is included. Benchmark results are written to the ignored `BenchmarkDotNet.Artifacts` directory.
