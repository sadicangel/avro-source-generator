param(
  [Parameter(Mandatory)][string]$PackagePath,
  [Parameter(Mandatory)][string]$SdkVersion,
  [string]$ExpectedRoslynApiVersion,
  [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$PackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$versions = [xml](Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Packages.props') -Raw)
$centralRoslynVersion = $versions.SelectSingleNode("/Project/ItemGroup/PackageVersion[@Include='Microsoft.CodeAnalysis.CSharp']").Version
$packProjectPath = Join-Path $repoRoot 'src/AvroSourceGenerator.Pack/AvroSourceGenerator.Pack.csproj'
$packProject = [xml](Get-Content -LiteralPath $packProjectPath -Raw)
$roslynVersions = @($packProject.Project.ItemGroup.ProjectReference | ForEach-Object {
  $generatorProjectPath = Join-Path (Split-Path $packProjectPath) $_.Include
  $generatorProject = [xml](Get-Content -LiteralPath $generatorProjectPath -Raw)
  $roslynVersion = $generatorProject.SelectSingleNode('/Project/PropertyGroup/GeneratorRoslynVersion').InnerText
  if (!$roslynVersion) { $roslynVersion = $centralRoslynVersion }
  ([version]$roslynVersion).ToString(2)
} | Sort-Object { [version]$_ })
if ($ExpectedRoslynApiVersion -eq 'latest') { $ExpectedRoslynApiVersion = $roslynVersions[-1] }

# Inspect the exact package that consumers will restore, including every analyzer dependency.
$sharedDependencies = @(
  'AvroSourceGenerator.Core', 'AvroSourceGenerator.Templating',
  'System.Buffers', 'System.Collections.Immutable', 'System.IO.Pipelines', 'System.Memory',
  'System.Runtime.CompilerServices.Unsafe', 'System.Text.Encodings.Web', 'System.Text.Json',
  'System.Threading.Tasks.Extensions', 'Microsoft.Bcl.AsyncInterfaces'
)
$variantDependencies = @('AvroSourceGenerator')
$archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
  $analyzers = @($archive.Entries | Where-Object FullName -Like 'analyzers/*' | ForEach-Object FullName)
  $expectedEntries = @($sharedDependencies | ForEach-Object { "analyzers/dotnet/cs/$_.dll" })
  $expectedEntries += @($roslynVersions | ForEach-Object {
    $api = $_
    $variantDependencies | ForEach-Object { "analyzers/dotnet/roslyn$api/cs/$_.dll" }
  })
  if (Compare-Object ($analyzers | Sort-Object) ($expectedEntries | Sort-Object)) {
    throw 'The package does not contain exactly the shared dependencies and two generator variants.'
  }
  if (!$archive.GetEntry('build/AvroSourceGenerator.props')) { throw 'Compiler-visible package properties are missing.' }
  $nuspec = @($archive.Entries | Where-Object FullName -Like '*.nuspec')
  if ($nuspec.Count -ne 1) { throw 'Expected one package manifest.' }
  $reader = [IO.StreamReader]::new($nuspec[0].Open())
  try { $manifest = [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
  $packageVersion = $manifest.package.metadata.version
}
finally { $archive.Dispose() }

# Each run gets its own SDK policy, projects, feed, and cache; root global.json cannot override it.
$workspace = Join-Path $repoRoot ("artifacts/compatibility/$SdkVersion/" + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($workspace) | Out-Null
if (!$OutputDirectory) { $OutputDirectory = Join-Path $workspace 'manifests' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$sdkPolicy = @{ sdk = @{ version = $SdkVersion; rollForward = 'disable'; allowPrerelease = $true } }
[IO.File]::WriteAllText((Join-Path $workspace 'global.json'), ($sdkPolicy | ConvertTo-Json -Depth 3))
Copy-Item -LiteralPath (Join-Path $repoRoot 'Directory.Packages.props') -Destination $workspace
$feed = Join-Path $workspace 'feed'
[IO.Directory]::CreateDirectory($feed) | Out-Null
Copy-Item -LiteralPath $PackagePath -Destination $feed
$cache = Join-Path $workspace 'packages'
$nugetConfig = Join-Path $workspace 'NuGet.Config'
$escapedFeed = [Security.SecurityElement]::Escape($feed)
$configuration = @"
<configuration>
  <packageSources>
    <clear />
    <add key="validation" value="$escapedFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="validation"><package pattern="AvroSourceGenerator" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@
[IO.File]::WriteAllText($nugetConfig, $configuration)

function Invoke-DotNet([string[]]$Arguments) {
  & dotnet @Arguments
  if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE." }
}

$passedConsumers = 0
$expectedConsumers = 6
$compilerVersion = $null
$selectedApi = $null
$reviewNote = $null
$failure = $null
Push-Location $workspace
try {
  $actualSdk = & dotnet --version
  if ($LASTEXITCODE -ne 0 -or $actualSdk.Trim() -ne $SdkVersion) {
    throw "Expected SDK $SdkVersion, got $actualSdk."
  }
  Write-Host "Validating SDK $actualSdk with package $packageVersion."
  $sdkMajor = [int]($SdkVersion.Split('.')[0])
  $modes = @('CSharp12', 'SdkDefault')
  if ($sdkMajor -ge 11) { $modes += 'SdkUnions' }
  $expectedConsumers = $modes.Count * 3
  foreach ($mode in $modes) {
    $modeDirectory = Join-Path $workspace $mode
    [IO.Directory]::CreateDirectory($modeDirectory) | Out-Null
    Copy-Item -LiteralPath $PSScriptRoot -Destination (Join-Path $modeDirectory 'Consumers') -Recurse
    $schemas = Join-Path $modeDirectory 'Schemas'
    [IO.Directory]::CreateDirectory($schemas) | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tests/Schemas') -File |
      Where-Object Extension -In @('.avsc', '.avpr', '.avdl') |
      Copy-Item -Destination $schemas
    $framework = if ($mode -eq 'CSharp12') { 'net10.0' } else { "net$sdkMajor.0" }
    $language = if ($mode -eq 'CSharp12') { '12.0' } elseif ($mode -eq 'SdkUnions') { 'preview' } else { 'default' }
    foreach ($library in @('Apache', 'Chr', 'None')) {
      $project = Join-Path $modeDirectory "Consumers/$library/$library.csproj"
      $compilerApi = & dotnet msbuild $project -nologo -getProperty:CompilerApiVersion
      if ($LASTEXITCODE -ne 0) { throw 'Cannot determine the consumer compiler API version.' }
      $compilerApi = @($compilerApi | Where-Object { $_ -match '^roslyn\d+\.\d+$' })
      if ($compilerApi.Count -ne 1) { throw 'Expected one consumer compiler API version.' }
      $compilerVersion = [version]$compilerApi[0].Replace('roslyn', '')
      if ($mode -eq 'CSharp12' -and $library -eq 'Apache' -and
          $ExpectedRoslynApiVersion -eq $roslynVersions[-1] -and $compilerVersion -gt [version]$roslynVersions[-1]) {
        $reviewNote = "Compiler Roslyn $compilerVersion is newer than generator target $($roslynVersions[-1]); review Roslyn updates."
        Write-Warning "$reviewNote Compatibility checks will validate the supported variant."
      }
      $selectedApi = @($roslynVersions | Where-Object { [version]$_ -le $compilerVersion } |
        Sort-Object { [version]$_ })[-1]
      if (!$selectedApi) { throw "No generator supports compiler $compilerApi." }
      if ($ExpectedRoslynApiVersion -and $selectedApi -ne $ExpectedRoslynApiVersion) {
        throw "Expected variant $ExpectedRoslynApiVersion for SDK $SdkVersion, got $selectedApi."
      }
      $properties = @(
        "-p:ValidationPackageVersion=$packageVersion",
        "-p:ConsumerTargetFramework=$framework",
        "-p:ConsumerLanguageVersion=$language",
        "-p:ConsumerPreviewFeatures=$(if ($mode -eq 'SdkUnions') { 'Unions' } else { 'None' })",
        "-p:ExpectedRoslynApiVersion=$selectedApi"
      )
      Invoke-DotNet -Arguments (@('restore', $project, '--configfile', $nugetConfig, '--packages', $cache) + $properties)
      Invoke-DotNet -Arguments (@('run', '--project', $project, '--configuration', 'Release', '--no-restore') + $properties)
      if ($mode -eq 'CSharp12') {
        $generatedRoot = Join-Path (Split-Path $project) 'obj/generated'
        $hashes = [ordered]@{}
        $files = @(Get-ChildItem -LiteralPath $generatedRoot -Recurse -Filter '*.Avro.g.cs' | Sort-Object Name)
        if ($files.Count -eq 0) { throw "No generated source was emitted for $library." }
        foreach ($file in $files) {
          $source = [IO.File]::ReadAllText($file.FullName).Replace("`r`n", "`n")
          $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($source)))
          $hashes[$file.Name] = $hash
        }
        [IO.File]::WriteAllText((Join-Path $OutputDirectory "$library.json"), ($hashes | ConvertTo-Json))
      }
      $passedConsumers++
    }
  }
}
catch {
  $failure = $_.Exception.Message
  throw
}
finally {
  Pop-Location
  $summary = @{
    Sdk = $SdkVersion
    CompilerRoslyn = if ($compilerVersion) { $compilerVersion.ToString(2) } else { 'Unknown' }
    GeneratorRoslyn = if ($selectedApi) { $selectedApi } else { 'Unknown' }
    PassedConsumers = $passedConsumers
    ExpectedConsumers = $expectedConsumers
    ReviewNote = $reviewNote
    Failure = $failure
  }
  [IO.File]::WriteAllText((Join-Path $OutputDirectory 'summary.json'), ($summary | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
}
