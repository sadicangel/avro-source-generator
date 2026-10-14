param(
  [string]$OutputDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) ('avro-docs-' + [guid]::NewGuid().ToString('N')))
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$versions = Get-Content -LiteralPath (Join-Path $repositoryRoot 'docs/_data/versions.json') -Raw | ConvertFrom-Json
$sdk = dotnet --list-sdks | ForEach-Object { ($_ -split ' ')[0] } |
  Where-Object { $_ -match '^10\.0\.\d+$' } |
  Sort-Object { [version]$_ } -Descending | Select-Object -First 1
if (!$sdk) { throw 'Install a .NET 10 SDK to validate the documented examples.' }

$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputRoot) { throw "Output directory already exists: $outputRoot" }
New-Item -ItemType Directory -Path $outputRoot | Out-Null

function New-Consumer($Name, $Library, $Packages, $Schemas, $Resolution = 'Strict') {
  $directory = Join-Path (Join-Path $outputRoot $version.id) $Name
  New-Item -ItemType Directory -Path (Join-Path $directory 'schemas') -Force | Out-Null
  $references = "<PackageReference Include=`"AvroSourceGenerator`" Version=`"$($version.package_version)`" PrivateAssets=`"all`" ExcludeAssets=`"runtime`" />"
  foreach ($package in $Packages.GetEnumerator()) {
    $references += "<PackageReference Include=`"$($package.Key)`" Version=`"$($package.Value)`" />"
  }
  @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AvroSourceGeneratorAvroLibrary>$Library</AvroSourceGeneratorAvroLibrary>
    <AvroSourceGeneratorReferenceResolution>$Resolution</AvroSourceGeneratorReferenceResolution>
  </PropertyGroup>
  <ItemGroup>
    $references
    <AdditionalFiles Include="schemas/*.avsc;schemas/*.avpr" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $directory 'Consumer.csproj') -Encoding utf8
  foreach ($schema in $Schemas) {
    Copy-Item -LiteralPath (Join-Path $examples $schema) -Destination (Join-Path $directory 'schemas')
  }
  # Do not inherit the checkout's frameworks, compiler references, or test runner.
  '<Project />' | Set-Content -LiteralPath (Join-Path $directory 'Directory.Build.props') -Encoding utf8
  '<Project />' | Set-Content -LiteralPath (Join-Path $directory 'Directory.Build.targets') -Encoding utf8
  '<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>' |
    Set-Content -LiteralPath (Join-Path $directory 'Directory.Packages.props') -Encoding utf8
  @{ sdk = @{ version = $sdk; rollForward = 'disable'; allowPrerelease = $false } } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $directory 'global.json') -Encoding utf8
  return $directory
}

function Invoke-Consumer($Directory, $ExpectedOutput, $ExpectedDiagnostic, [switch]$BuildOnly) {
  Push-Location $Directory
  try {
    Write-Host "Verifying $($version.label): $(Split-Path $Directory -Leaf) using SDK $sdk"
    $command = if ($BuildOnly) { 'build' } else { 'run' }
    $output = @(dotnet $command --configuration Release 2>&1 | ForEach-Object { "$_" })
    $result = $LASTEXITCODE
    $output | Set-Content -LiteralPath (Join-Path $Directory 'verification.log') -Encoding utf8
    if ($ExpectedDiagnostic) {
      if ($result -eq 0 -or ($output -join "`n") -notmatch $ExpectedDiagnostic) {
        $output | Write-Host
        throw "Expected failing build with $ExpectedDiagnostic."
      }
      Write-Host "Confirmed expected $ExpectedDiagnostic failure."
    }
    elseif ($result -ne 0 -or ($ExpectedOutput -and $output -notcontains $ExpectedOutput)) {
      $output | Write-Host
      throw "Consumer failed or produced unexpected output (exit $result)."
    }
    else { Write-Host 'Passed.' }
  }
  finally { Pop-Location }
}

foreach ($version in $versions) {
  $examples = Join-Path $repositoryRoot "docs/versions/$($version.id)/_includes/examples"
  $apache = @{ 'Apache.Avro' = '1.12.2' }
  $consumer = New-Consumer 'QuickStart' 'Apache' $apache @('user.avsc')
  Copy-Item -LiteralPath (Join-Path $examples 'Program.cs') -Destination (Join-Path $consumer 'Program.cs')
  Invoke-Consumer $consumer 'Ada <ada@example.com>'

  $consumer = New-Consumer 'ApacheKafka' 'Apache' (@{ 'Apache.Avro' = '1.12.2'; 'Confluent.SchemaRegistry.Serdes.Avro' = '2.15.1' }) @('user.avsc')
  Copy-Item -LiteralPath (Join-Path $examples 'ApacheKafka.cs') -Destination (Join-Path $consumer 'Program.cs')
  Invoke-Consumer $consumer -BuildOnly

  # Published guides retain their own examples, including the Chr integration in 0.7.x.
  if (Test-Path -LiteralPath (Join-Path $examples 'ChrKafka.cs')) {
    $consumer = New-Consumer 'ChrKafka' 'Chr' (@{ 'Chr.Avro.Confluent' = '10.13.1' }) @('user.avsc')
    Copy-Item -LiteralPath (Join-Path $examples 'ChrKafka.cs') -Destination (Join-Path $consumer 'Program.cs')
    Invoke-Consumer $consumer -BuildOnly
  }

  $program = 'var order = new example.Order { Customer = new example.Customer { Id = "42" } }; Console.WriteLine(order.Customer.Id);'
  $consumer = New-Consumer 'ProtocolInternalStrict' 'Apache' $apache @('standalone-protocol.avpr')
  $program | Set-Content -LiteralPath (Join-Path $consumer 'Program.cs') -Encoding utf8
  Invoke-Consumer $consumer '42'

  $consumer = New-Consumer 'ProtocolExternalStrict' 'Apache' $apache @('customer.avsc', 'external-protocol.avpr')
  $program | Set-Content -LiteralPath (Join-Path $consumer 'Program.cs') -Encoding utf8
  Invoke-Consumer $consumer -ExpectedDiagnostic 'AVROSG0005' -BuildOnly

  $consumer = New-Consumer 'ProtocolExternalDeferred' 'Apache' $apache @('customer.avsc', 'external-protocol.avpr') 'Deferred'
  $program | Set-Content -LiteralPath (Join-Path $consumer 'Program.cs') -Encoding utf8
  Invoke-Consumer $consumer '42'
}
Write-Host "All documented consumers passed. Kafka examples compiled; running them requires Kafka and Schema Registry. Consumers retained at $outputRoot"
