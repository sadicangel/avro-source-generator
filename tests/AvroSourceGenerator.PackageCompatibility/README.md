# Package compatibility checks

These consumers restore the locally packed NuGet package, rather than referencing a generator project.
They verify the exact analyzer files and dependencies in the package, selection of exactly one compatible generator,
compiler-visible package properties, and generated models for Apache.Avro, Chr.Avro, and no runtime library.
Shared fixtures exercise schemas, protocols, IDL imports, collections, and union interfaces without Docker.

Each SDK runs every consumer twice: with C# 12 targeting .NET 10, and with its default C# version targeting its own .NET version.
The C# 12 runs emit source hashes for comparison across SDKs. Unit snapshots remain a single suite on the latest Roslyn;
Kafka and Schema Registry tests live in the separate roundtrip projects and run once per library.

CI also passes `-IncludeNet8Consumer` to the SDK 10.0.100 run. This builds one library-free consumer with C# 12 targeting
.NET 8, checking that generated models still compile for an older target framework using a supported compiler host.
This isolated check does not require SDK 8 or the .NET 8 runtime; solution tests and tools remain on .NET 10.

The unsuffixed generator uses the central `Microsoft.CodeAnalysis.CSharp` package version.
Only the fixed Roslyn 5.0 baseline project overrides it. Package analyzer directories use the major and minor numbers of that configured version.

The package stores Core, Templating, and nine common runtime dependencies, including Immutable, once in `analyzers/dotnet/cs/`.
Each `analyzers/dotnet/roslynX.Y/cs/` folder contains only the generator.
The checks verify all 13 packaged DLLs and that each consumer receives the 11 shared dependencies plus exactly one generator variant.

All source projects use the central `System.Collections.Immutable` version without overrides.
The package consumers verify that the shared assemblies and Immutable APIs work with every supported compiler host,
including SDK 10.0.100. Increasing Immutable's major version requires checking that minimum host again.

Install SDK 10.0.100, the latest stable SDK, and the latest preview SDK, plus the .NET 10 runtime.
Run from the repository root after a Release solution build:

```powershell
dotnet pack src/AvroSourceGenerator.Pack/AvroSourceGenerator.Pack.csproj --no-build --configuration Release -p:PackageVersion=0.0.0-validation --output artifacts/packages
$package = 'artifacts/packages/AvroSourceGenerator.0.0.0-validation.nupkg'
./tests/AvroSourceGenerator.PackageCompatibility/Run-Compatibility.ps1 -PackagePath $package -SdkVersion 10.0.100 -ExpectedRoslynApiVersion 5.0 -OutputDirectory artifacts/manifests/net10 -IncludeNet8Consumer
```

Run the same script for the exact installed latest stable and preview SDK versions, writing to separate manifest directories.
For the preview SDK, pass `-ExpectedRoslynApiVersion latest`; this also checks that the compiler API matches the latest
configured generator target, so CI catches new Roslyn baselines that need review. The stable SDK's selected variant
is determined from its compiler API version. Then compare all outputs:

```powershell
./tests/AvroSourceGenerator.PackageCompatibility/Compare-GeneratedSources.ps1 -ManifestDirectory artifacts/manifests
```

The script creates an isolated workspace, exact-version `global.json`, local package feed, and package cache under `artifacts/compatibility`.
External consumer dependencies restore from nuget.org. The generator itself must restore from the local package;
a published package cannot satisfy the validation run. The main solution's SDK policy and NuGet configuration are unchanged.

The build workflow builds and packs once. Unit and fast integration tests, Docker roundtrips, and package compatibility
checks then run in parallel jobs using those build outputs. Compatibility checks resolve the newest stable and preview
SDKs on every push and pull request and compare the generated sources in the same job.
The release workflow builds, tests stable releases, and packs the release version separately before publishing.
