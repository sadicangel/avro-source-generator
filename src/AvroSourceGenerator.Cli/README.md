# Avro Source Generator CLI

Generate C# models from Avro schema (`.avsc`), protocol (`.avpr`), and IDL (`.avdl`) files on demand. Supports Apache.Avro, Chr.Avro, and standalone models using the same compiler and templates as AvroSourceGenerator.

Requires the .NET 10 runtime. The CLI and source generator are released with the same NuGet version.

```powershell
dotnet tool install --local AvroSourceGenerator.Cli --version <package-version>
dotnet tool run avro-sg -- compile "./schemas/**/*.avsc" "./schemas/**/*.avdl" --output ./Generated --library Apache
```

Use the actual package version in place of `<package-version>`. Commit the local tool manifest and run `dotnet tool restore` on other machines. Global installation is also supported:

```powershell
dotnet tool install --global AvroSourceGenerator.Cli --version <package-version>
avro-sg compile ./schemas --output ./Generated --library Chr
```

Quote glob patterns so the tool expands them consistently across shells. Directories include supported files recursively. All imported files must be selected as inputs; imports do not discover additional files automatically.

Existing files are errors by default. Other outputs continue, and the command returns a nonzero exit code if any errors occur. Explicitly allow replacement with `--force`, or verify existing output without writing with `--check`:

```powershell
avro-sg compile ./schemas --output ./Generated --library Apache --force
avro-sg compile ./schemas --output ./Generated --library Apache --check
avro-sg compile --help
```

Neither command removes stale or unrelated files. `--check` ignores extra files and BOM/newline differences. `--force` and `--check` cannot be combined.

Defaults are standalone models, C# 14, public records when supported, strict references, duplicate errors, and no preview opt-ins. Use `--language-version` to match the consuming compiler; preview unions additionally require `--preview-features Unions` and a capable consuming compiler.

See the [preview CLI guide](https://sadicangel.github.io/avro-source-generator/preview/cli/) for every option and diagnostic/exit-code behavior. C# to Avro reverse compilation is future work.
