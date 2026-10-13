---
title: Command-line tool
nav_order: 9
---

# Command-line tool

The `avro-sg compile` command generates C# models on demand using the same compiler and templates as the source generator. Use it to commit generated models, review generation changes, or generate models in CI. The tool requires the .NET 10 runtime; generated syntax must also be supported by the consuming project's compiler.

## Install

Install a repository-local tool, replacing `<package-version>` with the matching published package version:

```powershell
dotnet tool install --local AvroSourceGenerator.Cli --version <package-version>
dotnet tool run avro-sg -- compile ./schemas --output ./Generated --library Apache
```

Commit the tool manifest. Other developers and CI can install that exact tool version with `dotnet tool restore`. Global installation is also supported:

```powershell
dotnet tool install --global AvroSourceGenerator.Cli --version <package-version>
avro-sg --version
```

## Select inputs

Supply one or more files, directories, or quoted glob patterns as positional arguments:

```powershell
avro-sg compile "./schemas/**/*.avsc" "./schemas/**/*.avdl" --output ./Generated --library Apache
```

Directories include `.avsc`, `.avpr`, and `.avdl` files recursively; directory links and junctions are not followed. Quote glob patterns to let the tool expand them consistently across shells. Relative paths are relative to the working directory. Missing files and expressions matching no supported files are errors.

Input expressions are processed in argument order, with matches within each expression sorted by normalized path. Overlapping selections include each file once. This determines which declaration wins with `--duplicate-resolution Ignore`.

Select every imported file, including transitive imports. Imports resolve only among selected inputs, relative to the importing file. See [schemas and imports]({{ '/schemas/' | relative_url }}).

## Options

| Option | Default | Values |
|:---|:---|:---|
| `--output`, `-o` | Required | Output directory |
| `--library` | `None` | `None`, `Apache`, `Chr`; `Auto` is explicitly rejected |
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

Language presets are `CSharp7_3`, `CSharp8`, `CSharp9`, `CSharp10`, `CSharp11`, `CSharp12`, `CSharp13`, `CSharp14`, and `CSharp15`. Individual flags are `NullableReferenceTypes`, `Records`, `InitOnlyProperties`, `RequiredProperties`, `RawStringLiterals`, `UnsafeAccessors`, and `Unions`; `None`, `All`, and `Latest` are also supported. Requested features are capped by the C# version. For example:

```powershell
avro-sg compile ./schemas --output ./Generated --library None --language-version 12 --language-features "NullableReferenceTypes,InitOnlyProperties" --record-declaration class
```

Native C# unions additionally require `--preview-features Unions` and `--language-version preview` or `15`, plus a consuming compiler/runtime capable of compiling them. Other generation options follow the [generator configuration rules]({{ '/configuration/' | relative_url }}).

## Existing files and CI

Existing output files are errors, even when identical. They remain untouched; other generated outputs continue. Add `--force` to explicitly permit replacement:

```powershell
avro-sg compile ./schemas --output ./Generated --library Apache --force
```

Identical files are not rewritten with `--force`. Neither stale files nor unrelated files are removed. Outputs use the generator's `<full-name>.Avro.g.cs` filenames and UTF-8 with BOM. Keep generated files separate from handwritten partial declarations, and avoid also generating the same types with the source generator in one compilation.

Check committed output in CI without modifying files:

```powershell
avro-sg compile ./schemas --output ./Generated --library Apache --check
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

C# to Avro compilation through `avro-sg reverse` is planned future work. It is not an available command in this release.
