# Repository guidance

## Project overview

Avro Source Generator is a C# incremental source generator for Avro schema (`.avsc`), protocol (`.avpr`), and IDL (`.avdl`) files. It can generate models for Apache Avro, Chr.Avro, or without a runtime library.

The solution uses the latest installed development SDK, including previews, selected by `global.json` with a minimum of SDK 10.0.401. The packaged generator supports consumers starting with SDK 10.0.100. Source projects target `netstandard2.0`; tests and tools use the target configured in the root `Directory.Build.props`. Package versions are managed centrally in `Directory.Packages.props`.

## Repository layout

- `src/AvroSourceGenerator.Core/` contains parsing, schemas, diagnostics, and the compiler pipeline. Keep it independent of Roslyn and rendering.
- `src/AvroSourceGenerator/` contains the Roslyn incremental generator, the latest Roslyn project, and shared build targets. `src/AvroSourceGenerator.Roslyn5_0/` builds the same sources against the minimum supported baseline; `src/AvroSourceGenerator.Pack/` packages both variants.
- `src/AvroSourceGenerator.Templating/` contains render models and Scriban templates.
- `tests/AvroSourceGenerator.UnitTests/` contains library-neutral unit and snapshot tests.
- `tests/AvroSourceGenerator.UnitTests.Apache/` and `tests/AvroSourceGenerator.UnitTests.Chr/` contain library-specific tests.
- `tests/AvroSourceGenerator.IntegrationTests*/` contains fast generated-model integration tests without Docker.
- `tests/AvroSourceGenerator.PackageCompatibility/` validates the packed generator against the minimum SDK 10.0.100 and the latest preview SDK.
- `tests/AvroSourceGenerator.RoundtripTests*/` contains Docker-based Kafka and Schema Registry roundtrips, run once per library rather than once per SDK.
- `tests/Schemas/` contains shared Avro fixtures.
- `samples/` contains consumer examples.
- `tools/` contains benchmarks and Windows ETW tracing utilities.

## Working conventions

- Inspect the relevant implementation and tests before editing. Keep changes focused and split large architectural work into reviewable batches.
- Preserve unrelated working-tree changes. Do not reset or discard user work.
- Do not change package versions or target frameworks unless the task requires it.
- Keep package compatibility checks on the fixed minimum SDK 10.0.100 and the latest preview SDK. When .NET 11 is GA and .NET 12 enters preview, add a fixed .NET 11 SDK baseline while retaining the minimum .NET 10 baseline and latest preview check. Newer compiler API versions prompt a Roslyn update review warning; analyzer selection, compilation, consumer execution, and generated-source parity must still pass.
- On Roslyn updates, keep the unsuffixed `AvroSourceGenerator` project on the latest supported Roslyn and preserve the explicit 5.0 baseline. Align the development SDK in `global.json` with the latest generator's compiler API; samples and integration tests reference that assembly directly. Review Roslyn versions in the generator projects and central package management, the central `System.Collections.Immutable` version and its compatibility with the minimum SDK 10.0.100 compiler host, unit-test compiler references, SDK matrix, and documentation. Numeric `analyzers/dotnet/roslynX.Y/cs` paths are derived from each version; there is no `latest` package directory. Verify package contents, analyzer selection, and generated-source parity across the SDK matrix before release. Core, Templating, and all common dependencies, including Immutable, are packaged once in `analyzers/dotnet/cs`; keep only the generator in each versioned folder. All source projects use the central Immutable version; verify minimum compiler-host compatibility before increasing its major version.
- Preserve valid-input behavior, generated output, import behavior, provenance, and deterministic diagnostic ordering unless the requested change intentionally updates a contract.
- Treat equality and hash codes as part of incremental-generator correctness. Add or update caching tests when a change affects pipeline inputs or outputs.
- Imports resolve only among supplied sources and relative to the importing file. Do not add implicit filesystem reads.
- Treat repository implementation APIs as internal, regardless of visibility or project. Refactor them directly and update callers and tests; do not retain compatibility wrappers solely because a type or member is public.
- Keep consumer-facing contracts—generated code, MSBuild properties, package integration, and diagnostics—compatible when practical. Update documentation and regression coverage when those contracts change.
- Do not include agent or AI attribution in branches, commits, PR titles, or PR descriptions.
- Do not prefix branches.

## Compiler safety

- Pass the caller's `CancellationToken` through parser and compiler stages, check it in potentially long operations, and never convert cancellation into a diagnostic.
- Keep exception-to-diagnostic handling narrow. Malformed user input should produce repository-owned diagnostics; cancellation and internal invariant failures should surface normally.
- Preserve source provenance when parsing, linking, and binding. Diagnostics should point to the source that owns the declaration, reference, or import.
- Preserve diagnostic IDs, messages, and ordering during refactors unless the change explicitly updates them. Review Roslyn descriptors and analyzer release notes for intentional diagnostic changes.

## Style and generated artifacts

- Follow `.editorconfig` and nearby code. C# uses four spaces, file-scoped namespaces, `System` usings first, and `var` where configured.
- Preserve existing encodings and line endings. C# files use UTF-8 BOM and CRLF. Verify snapshots use the special rules in `.editorconfig` and `.gitattributes`.
- Treat generated source and `.verified.*` snapshots as contracts. Review snapshot differences before accepting them; do not bulk-accept unexplained changes.

## Build and test

Run commands from the repository root. For normal implementation work, build Release and run the three unit suites and both fast integration suites. Unit and snapshot tests run only against the latest generator variant:

```powershell
dotnet restore avro-source-generator.slnx
dotnet build avro-source-generator.slnx --no-restore --configuration Release
dotnet test --project tests/AvroSourceGenerator.UnitTests/AvroSourceGenerator.UnitTests.csproj --no-build --configuration Release
dotnet test --project tests/AvroSourceGenerator.UnitTests.Apache/AvroSourceGenerator.UnitTests.Apache.csproj --no-build --configuration Release
dotnet test --project tests/AvroSourceGenerator.UnitTests.Chr/AvroSourceGenerator.UnitTests.Chr.csproj --no-build --configuration Release
dotnet test --project tests/AvroSourceGenerator.IntegrationTests.Apache/AvroSourceGenerator.IntegrationTests.Apache.csproj --no-build --configuration Release
dotnet test --project tests/AvroSourceGenerator.IntegrationTests.Chr/AvroSourceGenerator.IntegrationTests.Chr.csproj --no-build --configuration Release
```

For generator build, dependency, or packaging changes, also pack `src/AvroSourceGenerator.Pack/AvroSourceGenerator.Pack.csproj` and run the SDK consumer matrix described in `tests/AvroSourceGenerator.PackageCompatibility/README.md`.

Before pushing code to `origin` or creating or updating a PR, also ensure Docker is available and run both roundtrip suites:

```powershell
docker info
dotnet test --project tests/AvroSourceGenerator.RoundtripTests.Apache/AvroSourceGenerator.RoundtripTests.Apache.csproj --no-build --configuration Release
dotnet test --project tests/AvroSourceGenerator.RoundtripTests.Chr/AvroSourceGenerator.RoundtripTests.Chr.csproj --no-build --configuration Release
```

`global.json` opts into Microsoft.Testing.Platform, so use `--project` for individual projects. Add focused regression tests for changed behavior. Documentation-only changes need a content review and `git diff --check`; they do not require a full build unless requested.

If restore, build, or tests cannot run, report the exact command and cause. Do not change dependencies, credentials, feeds, or test expectations merely to bypass an environment failure.

Before handing off, inspect the final diff for unrelated changes and formatting churn, then report what changed and which checks ran.
