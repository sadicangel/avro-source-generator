# Avro Source Generator CLI

Generate C# models from Avro schema (`.avsc`), protocol (`.avpr`), and IDL (`.avdl`) files on demand. Supports Apache.Avro and standalone models using the same compiler and templates as AvroSourceGenerator.

Requires the .NET 10 runtime. The CLI and source generator are released with the same NuGet version.

```powershell
dotnet tool install --local AvroSourceGenerator.Cli --version <package-version>
dotnet tool run avro-sg -- cs "./schemas/**/*.avsc" "./schemas/**/*.avdl" --output ./Generated --library Apache
```

Use the actual package version in place of `<package-version>`. Commit the local tool manifest and run `dotnet tool restore` on other machines. Global installation is also supported:

```powershell
dotnet tool install --global AvroSourceGenerator.Cli --version <package-version>
avro-sg cs ./schemas --output ./Generated --library Apache
```

Quote glob patterns so the tool expands them consistently across shells. Directories include supported files recursively. All imported files must be selected as inputs; imports do not discover additional files automatically.

Existing files are errors by default. Other outputs continue, and the command returns a nonzero exit code if any errors occur. Explicitly allow replacement with `--force`, or verify existing output without writing with `--check`:

```powershell
avro-sg cs ./schemas --output ./Generated --library Apache --force
avro-sg cs ./schemas --output ./Generated --library Apache --check
avro-sg cs --help
```

Neither command removes stale or unrelated files. `--check` ignores extra files and BOM/newline differences. `--force` and `--check` cannot be combined.

Defaults are standalone models, C# 14, public records when supported, strict references, duplicate errors, and no preview opt-ins. Use `--language-version` to match the consuming compiler; preview unions additionally require `--preview-features Unions` and a capable consuming compiler.

The `avsc` and `avdl` commands generate Avro schema JSON and Avro IDL from a C# source file using its project or standalone .NET 10 references. Projects must be restored first.

```powershell
avro-sg avsc ./Contracts/OrderPlaced.cs -o ./Schemas
avro-sg avdl ./Contracts/OrderPlaced.cs -o ./Schemas --type Contracts.OrderPlaced --documentation xml
```

Use `--force` to replace changed schemas or `--check` to verify them without writing.

See the [preview CLI guide](https://sadicangel.github.io/avro-source-generator/preview/cli/) for options and diagnostic and exit-code behavior.

Chr.Avro support is removed in 0.8.0. `--library Chr` is rejected before writing files; migrate to Apache.Avro, explicitly select `None` for standalone models, or remain on 0.7.x.
