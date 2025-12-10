param(
    [Parameter(Mandatory = $true)]
    [string]$BaseVersion   # Example: 0.5.2 or v0.5.2
)

# Ensure we're in a git repo
git rev-parse --is-inside-work-tree 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Not inside a git repository."
    exit 1
}

# Normalize base version (strip leading 'v' if present)
$NormalizedBaseVersion = $BaseVersion.TrimStart('v')

# Validate semantic version
try {
    $BaseVersionObj = [version]$NormalizedBaseVersion
}
catch {
    Write-Error "Base version '$BaseVersion' is not a valid semantic version (e.g. 0.5.2 or v0.5.2)."
    exit 1
}

# ---- 1. Determine preview number for this base version ----
# Count existing preview tags for this base version (support with or without 'v' in the tag)
$PreviewTags = git tag --list "$NormalizedBaseVersion-preview.*" "v$NormalizedBaseVersion-preview.*" 2>$null
$PreviewCount = if ($PreviewTags) { ($PreviewTags | Measure-Object).Count } else { 0 }
$NextPreview = $PreviewCount + 1

# ---- 2. Find base / previous version tag for commit counting ----
$AllTags = git tag --list 2>$null

$TagInfos = @()
foreach ($t in $AllTags) {
    $norm = $t.TrimStart('v')
    $verObj = $null
    if ([version]::TryParse($norm, [ref]$verObj)) {
        $TagInfos += [pscustomobject]@{
            Tag     = $t
            Version = $verObj
        }
    }
}

$ExactBaseTag = $TagInfos |
Where-Object { $_.Version -eq $BaseVersionObj } |
Select-Object -First 1

$RangeStartTag = $null

if ($ExactBaseTag) {
    # Tag for this base version already exists
    $RangeStartTag = $ExactBaseTag.Tag
}
else {
    # No exact base tag – use the highest tag below this version (e.g. v0.5.1 for base 0.5.2)
    $PreviousTag = $TagInfos |
    Where-Object { $_.Version -lt $BaseVersionObj } |
    Sort-Object Version -Descending |
    Select-Object -First 1

    if ($PreviousTag) {
        $RangeStartTag = $PreviousTag.Tag
    }
    else {
        # No previous version tags at all – fall back to entire history
        $RangeStartTag = $null
    }
}

# ---- 3. Compute commit count ----
if ($RangeStartTag) {
    $CommitsAheadRaw = git rev-list --count "$RangeStartTag..HEAD" 2>$null
}
else {
    $CommitsAheadRaw = git rev-list --count HEAD 2>$null
}

if ($LASTEXITCODE -ne 0 -or -not $CommitsAheadRaw) {
    Write-Error "Failed to compute commit count."
    exit 1
}

$CommitsAhead = [int]$CommitsAheadRaw

# ---- 4. Branch name sanitization ----
$BranchName = git rev-parse --abbrev-ref HEAD
$SanitizedBranch = $BranchName -replace '[^a-zA-Z0-9\-\.]', '-'

# ---- 5. Build final preview version (NuGet-safe, no build metadata) ----
# Format: 0.5.2-preview.<previewIndex>.<branch>.<commitsAhead>
$FinalVersion = "$NormalizedBaseVersion-preview.$NextPreview.$SanitizedBranch.$CommitsAhead"

Write-Host ""
Write-Host "Base version:          $BaseVersion"
if ($ExactBaseTag) {
    Write-Host "Base tag found:        $($ExactBaseTag.Tag)"
}
elseif ($RangeStartTag) {
    Write-Host "Base tag not found."
    Write-Host "Using previous tag:    $RangeStartTag"
}
else {
    Write-Host "No previous version tags found. Using entire history for commit count."
}
Write-Host "Commits since tag:     $CommitsAhead"
Write-Host ""

Write-Host "Preview version:"
Write-Host "  $FinalVersion"
Write-Host ""

$Sha = git rev-parse --short HEAD
Write-Host "From commit:"
Write-Host "  $Sha"
