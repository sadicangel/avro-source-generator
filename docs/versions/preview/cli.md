---
title: Command-line tool
nav_order: 9
---

# Command-line tool

The `avro-sg cs` command generates C# models on demand using the same compiler and templates as the source generator. Use it to commit generated models, review generation changes, or generate models in CI. The tool requires the .NET 10 runtime. The consuming project's compiler must support the generated C# syntax.

## Install

Install a repository-local tool, replacing `<package-version>` with the matching published package version:

```powershell
dotnet tool install --local AvroSourceGenerator.Cli --version <package-version>
dotnet tool run avro-sg -- cs ./schemas --output ./Generated --library Apache
```

Commit the tool manifest. Other developers and CI can install that exact tool version with `dotnet tool restore`. Global installation is also supported:

```powershell
dotnet tool install --global AvroSourceGenerator.Cli --version <package-version>
avro-sg --version
```

## Select inputs

Supply one or more files, directories, or quoted glob patterns as positional arguments:

```powershell
avro-sg cs "./schemas/**/*.avsc" "./schemas/**/*.avdl" --output ./Generated --library Apache
```

Directories include `.avsc`, `.avpr`, and `.avdl` files recursively; directory links and junctions are not followed. Quote glob patterns to let the tool expand them consistently across shells. Relative paths are relative to the working directory. Missing files and expressions matching no supported files are errors.

Input expressions are processed in argument order, with matches within each expression sorted by normalized path. Overlapping selections include each file once. This determines which declaration wins with `--duplicate-resolution Ignore`.

Select every imported file, including transitive imports. Imports resolve only among selected inputs, relative to the importing file. See [schemas and imports]({{ '/schemas/' | relative_url }}).

## Options

| Option | Default | Values |
|:---|:---|:---|
| `--output`, `-o` | Required | Output directory |
| `--library` | `None` | `None`, `Apache`; `Auto` is explicitly rejected |
| `--language-version` | `14` | `7.3`, `8` through `15`, `preview` |
| `--language-features` | `All` | Existing presets or comma-separated feature names |
| `--preview-features` | `None` | `None`, `Unions` |
| `--access-modifier` | `Public` | `Public`, `Internal` |
| `--record-declaration` | `record` when supported | `record`, `class` |
| `--reference-resolution` | `Strict` | `Strict`, `Deferred` |
| `--duplicate-resolution` | `Error` | `Error`, `Ignore` |
| `--force`, `-f` | Disabled | Allow replacement of existing output files |
| `--check` | Disabled | Compare expected output without writing |
| `--help`, `-h` | — | Show command help |

Root `--version` reports the package version from the tool assembly. Generated models also record that package version in `GeneratedCodeAttribute`, matching the source generator.

Enum names are case-insensitive; unknown names and numeric enum values are rejected. `Auto` requires a consuming compilation, which the standalone CLI does not have.

`--library Chr` fails before writing files and explains the supported alternatives. See [migration guidance]({{ '/libraries/#migrating-from-chravro' | relative_url }}).

Language presets are `CSharp7_3`, `CSharp8`, `CSharp9`, `CSharp10`, `CSharp11`, `CSharp12`, `CSharp13`, `CSharp14`, and `CSharp15`. Individual flags are `NullableReferenceTypes`, `Records`, `InitOnlyProperties`, `RequiredProperties`, `RawStringLiterals`, `UnsafeAccessors`, and `Unions`; `None`, `All`, and `Latest` are also supported. Requested features are capped by the C# version. For example:

```powershell
avro-sg cs ./schemas --output ./Generated --library None --language-version 12 --language-features "NullableReferenceTypes,InitOnlyProperties" --record-declaration class
```

Native C# unions additionally require `--preview-features Unions` and `--language-version preview` or `15`, plus a consuming compiler/runtime capable of compiling them. Other generation options follow the [generator configuration rules]({{ '/configuration/' | relative_url }}).

## Existing files and CI

Existing output files are errors, even when identical. They remain untouched; other generated outputs continue. Add `--force` to explicitly permit replacement:

```powershell
avro-sg cs ./schemas --output ./Generated --library Apache --force
```

Identical files are not rewritten with `--force`. Neither stale files nor unrelated files are removed. Outputs use the generator's `<full-name>.Avro.g.cs` filenames and UTF-8 with BOM. Keep generated files separate from handwritten partial declarations, and avoid also generating the same types with the source generator in one compilation.

On Windows, generated filenames are compared case-insensitively. If two schemas map to the same output path, the command fails before writing any files, including with `--force`.

Check committed output in CI without modifying files:

```powershell
avro-sg cs ./schemas --output ./Generated --library Apache --check
```

`--check` reports missing or changed expected files, ignoring BOM and line-ending differences. It does not create the output directory and ignores extra files, including stale generated files. It cannot be combined with `--force`.

## Diagnostics and exit codes

Compiler diagnostics retain source-generator IDs, messages, severity, ordering, and one-based file locations. Diagnostics and filesystem errors go to stderr; generated paths and help go to stdout. Independent valid files can still generate when another input has errors; files depending on invalid declarations are suppressed.

| Exit code | Meaning |
|:---|:---|
| `0` | Success; warnings alone do not fail |
| `1` | Compilation/filesystem errors or check differences |
| `2` | Invalid invocation or input selection |
| `130` | Cancellation |

## Generate Avro from C#

Use `avsc` for Avro schema JSON and `avdl` for Avro IDL. Both commands compile a single C# file and use its resolved types to build Avro schemas. The selected writer serializes each schema to UTF-8 text and the CLI writes it to the directory specified by `--output` (`-o`). Output files use the Avro full name and format extension, for example `Contracts.OrderPlaced.avsc`.

```powershell
avro-sg avsc ./Contracts/OrderPlaced.cs -o ./Schemas
avro-sg avdl ./Shared/OrderPlaced.cs -o ./Schemas --project ./Contracts/Contracts.csproj --framework net10.0
avro-sg avdl ./Contracts/OrderPlaced.cs -o ./Schemas --type Contracts.OrderPlaced --documentation xml --check
```

| Option | Behavior |
|:---|:---|
| `--type` | Select a root by its fully qualified C# name. The type must be declared in the input file. Referenced types are included in the selected schema. |
| `--documentation none` | Omit documentation. This is the default. |
| `--documentation xml` | Read C# XML `<summary>` comments on types and properties. Positional record properties also use the record's matching `<param>` comments. |
| `--force`, `-f` | Replace changed output files. Existing files are errors by default. |
| `--check` | Compare expected files without writing. Extra files are ignored. This option cannot be combined with `--force`. |

The tool searches from the file's directory upward and uses the first directory containing a project that compiles the file. If multiple projects in that directory include the file, select one with `--project`. Use `--project` for a linked file whose project is outside the file's parent directories. Multi-targeted projects require `--framework`.

Project mode uses the project's references and compiler settings. Install the project's .NET SDK and restore its dependencies beforehand. If no project includes the file, the tool compiles it on its own with .NET 10 reference assemblies, C# 14, and nullable reference types enabled. Standalone input is a plain C# file.

If the CLI reports missing .NET reference assemblies, run `dotnet restore <project.csproj>` and verify that the target framework's reference pack is available.

C# compilation errors prevent schema generation. Compiler diagnostics go to stderr, and the command prints each generated file path to stdout. The exit codes above also apply to `avsc` and `avdl`.

Each output includes the named types it references. When compiling an emitted IDL file with mutually recursive types using `cs`, pass `--reference-resolution Deferred` to allow the forward reference. When compiling multiple outputs that include the same type, pass `--duplicate-resolution Ignore`.

### C# type mappings

Nullable value types and annotated nullable reference types add a `null` branch to the Avro type. Arrays and enumerable collections map to Avro arrays, `byte[]` maps to `bytes`, and dictionaries with string keys map to Avro maps. A property whose type cannot be represented reports an error at its C# declaration.

| C# | Avro logical type |
|:---|:---|
| `Guid` | `uuid` over `string` |
| `DateOnly` | `date` over `int` |
| `TimeOnly` | `time-millis` over `int` |
| `DateTime`, `DateTimeOffset` | `timestamp-millis` over `long` |
| `decimal` | `decimal` over `bytes` with precision 57 and scale 28 |

Millisecond logical types store millisecond precision. `timestamp-millis` represents a UTC instant and discards the original offset. Decimal precision and scale cover the full numeric range and fractional scale of C# `decimal`.
