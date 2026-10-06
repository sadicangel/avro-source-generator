param([Parameter(Mandatory)][string]$ManifestDirectory)

$ErrorActionPreference = 'Stop'
foreach ($library in @('Apache', 'Chr', 'None')) {
  $manifests = @(Get-ChildItem -LiteralPath $ManifestDirectory -Recurse -Filter "$library.json")
  if ($manifests.Count -lt 2) { throw "Need at least two SDK manifests for $library." }
  $expected = (Get-Content -LiteralPath $manifests[0].FullName -Raw | ConvertFrom-Json -AsHashtable)
  foreach ($manifest in $manifests | Select-Object -Skip 1) {
    $actual = Get-Content -LiteralPath $manifest.FullName -Raw | ConvertFrom-Json -AsHashtable
    if ($actual.Count -ne $expected.Count) { throw "Generated file count differs in $($manifest.FullName)." }
    foreach ($file in $expected.Keys) {
      if ($actual[$file] -ne $expected[$file]) { throw "Generated source $file differs in $($manifest.FullName)." }
    }
  }
}
Write-Host 'Generated sources match across SDKs for Apache, Chr, and library-free generation.'
