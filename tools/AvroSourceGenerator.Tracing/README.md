# AvroSourceGenerator tracing

This project displays the duration of `AvroSourceGenerator` executions reported by Roslyn's `Microsoft-CodeAnalysis-General` event provider. It is useful for observing generator activity during real command-line or IDE builds, including whether an incremental edit causes the generator to run again.

The tracer uses an ETW session and therefore runs on Windows. Depending on local tracing permissions, the terminal may need to be elevated.

Start the tracing session in one terminal:

```powershell
dotnet run --project tools/AvroSourceGenerator.Tracing -c Release
```

Then build a project that uses the source generator from another terminal. Use `--no-incremental` when an ordinary build is skipped as up to date:

```powershell
dotnet build samples/AvroSourceGenerator.ApacheAvro -c Release --no-incremental
```

Each completed generator execution is printed in milliseconds:

```text
AvroSourceGenerator.AvroSourceGenerator: 125ms
```

Press <kbd>Ctrl</kbd>+<kbd>C</kbd> in the tracing terminal to stop the session cleanly.

The reported duration is Roslyn's end-to-end generator execution time. It does not include the rest of compilation and does not report allocations, per-stage timings, or statistically stable comparisons. Use `AvroSourceGenerator.Benchmarks` for repeatable current-versus-GA time and allocation measurements.
