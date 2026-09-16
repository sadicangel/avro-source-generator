# Repository guidance

## Project overview

Avro Source Generator is a C# incremental source generator for Avro schema (`.avsc`), protocol (`.avpr`), and IDL (`.avdl`) files. It can generate models for Apache Avro, Chr.Avro, or without a runtime library.

The solution uses the SDK selected by `global.json`. Source projects target `netstandard2.0`; tests and tools use the target configured in the root `Directory.Build.props`. Package versions are managed centrally in `Directory.Packages.props`.

## Repository layout

- `src/AvroSourceGenerator.Core/` contains parsing, schemas, diagnostics, and the compiler pipeline. Keep it independent of Roslyn and rendering.
- `src/AvroSourceGenerator/` contains the Roslyn incremental generator and project configuration.
- `src/AvroSourceGenerator.Templating/` contains render models and Scriban templates.
- `tests/AvroSourceGenerator.Tests/` contains library-neutral unit and snapshot tests.
- `tests/AvroSourceGenerator.Tests.Apache/` and `tests/AvroSourceGenerator.Tests.Chr/` contain library-specific tests.
- `tests/AvroSourceGenerator.IntegrationTests*/` contains Docker-based Kafka and Schema Registry integration tests.
- `tests/Schemas/` contains shared Avro fixtures.
- `samples/` contains consumer examples.
- `tools/` contains benchmarks and Windows ETW tracing utilities.

## Working conventions

- Inspect the relevant implementation and tests before editing. Keep changes focused and split large architectural work into reviewable batches.
- Preserve unrelated working-tree changes. Do not reset or discard user work.
- Do not change package versions or target frameworks unless the task requires it.
- Preserve valid-input behavior, generated output, import behavior, provenance, and deterministic diagnostic ordering unless the requested change intentionally updates a contract.
- Treat equality and hash codes as part of incremental-generator correctness. Add or update caching tests when a change affects pipeline inputs or outputs.
- Imports resolve only among supplied sources and relative to the importing file. Do not add implicit filesystem reads.
- Keep public API changes source-compatible when practical. Update API and regression coverage when public behavior changes.
- Do not include agent or AI attribution in branches, commits, PR titles, or PR descriptions.

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

Run commands from the repository root. For normal implementation work, build Release and run the three unit suites:

```powershell
dotnet restore avro-source-generator.slnx
dotnet build avro-source-generator.slnx --no-restore --configuration Release
dotnet test --project tests/AvroSourceGenerator.Tests/AvroSourceGenerator.Tests.csproj --no-build --configuration Release
dotnet test --project tests/AvroSourceGenerator.Tests.Apache/AvroSourceGenerator.Tests.Apache.csproj --no-build --configuration Release
dotnet test --project tests/AvroSourceGenerator.Tests.Chr/AvroSourceGenerator.Tests.Chr.csproj --no-build --configuration Release
```

Before pushing code to `origin` or creating or updating a PR, also ensure Docker is available and run both integration suites:

```powershell
docker info
dotnet test --project tests/AvroSourceGenerator.IntegrationTests.Apache/AvroSourceGenerator.IntegrationTests.Apache.csproj --no-build --configuration Release
dotnet test --project tests/AvroSourceGenerator.IntegrationTests.Chr/AvroSourceGenerator.IntegrationTests.Chr.csproj --no-build --configuration Release
```

`global.json` opts into Microsoft.Testing.Platform, so use `--project` for individual projects. Add focused regression tests for changed behavior. Documentation-only changes need a content review and `git diff --check`; they do not require a full build unless requested.

If restore, build, or tests cannot run, report the exact command and cause. Do not change dependencies, credentials, feeds, or test expectations merely to bypass an environment failure.

Before handing off, inspect the final diff for unrelated changes and formatting churn, then report what changed and which checks ran.
