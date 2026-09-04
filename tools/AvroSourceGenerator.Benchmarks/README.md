# AvroSourceGenerator benchmarks

This project compares the current local source-generator project with the latest stable NuGet release. The GA baseline is intentionally pinned in `AvroSourceGenerator.Benchmarks.csproj` so a report remains reproducible; update the `LastGaVersion` property when a new stable release is published. Preview releases are never selected implicitly.

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

Run all benchmarks:

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release --
```

For a faster, lower-confidence local check, add `--job short` after the final `--`.

Run only one scenario:

```powershell
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter "*FullGenerationBenchmarks*"
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter "*IncrementalGenerationBenchmarks*"
dotnet run --project tools/AvroSourceGenerator.Benchmarks -c Release -- --filter "*ReferencedIncrementalGenerationBenchmarks*"
```

BenchmarkDotNet reports `LastGa` as the baseline. For `CurrentTip`, a time or allocation ratio above `1.00` is a regression and a ratio below `1.00` is an improvement. Focus on ratios larger than the reported noise rather than small single-run differences, and compare results on the same idle machine and runtime.

No CI or pipeline integration is included. Benchmark results are written to the ignored `BenchmarkDotNet.Artifacts` directory.
