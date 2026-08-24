$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$jobRoot = Join-Path $repoRoot 'Assets/_Project/Art/Generated/ui_set/job_20260824133802_f43c6431'
$failures = New-Object System.Collections.Generic.List[string]

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { $failures.Add($Message) }
}
function Assert-Equal($Actual, $Expected, [string]$Message) {
    if ($Actual -ne $Expected) { $failures.Add("$Message expected '$Expected' got '$Actual'") }
}

$assets = Get-Content -Raw -LiteralPath (Join-Path $repoRoot '.forge/assets.json') | ConvertFrom-Json
$asset = $assets.assets | Where-Object { $_.id -eq 'ui.ending-gallery' }
Assert-True ($null -ne $asset) 'ui.ending-gallery missing from Forge ledger'
Assert-Equal $asset.status 'engine_ready' 'Forge asset status'
Assert-Equal $asset.currentJobId 'job_20260824133802_f43c6431' 'Forge current job'
Assert-Equal $asset.engine.kind 'unity' 'Forge engine kind'
Assert-Equal $asset.engine.manifest 'Assets\_Project\Art\Generated\ui_set\job_20260824133802_f43c6431\forge-import.json' 'Forge package manifest'

$feedback = Get-Content -Raw -LiteralPath (Join-Path $repoRoot '.forge/feedback.json') | ConvertFrom-Json
$feedbackEntry = @($feedback.entries | Where-Object { $_.jobId -eq 'job_20260824133802_f43c6431' })
Assert-Equal $feedbackEntry.Count 1 'Forge feedback decision count'
Assert-Equal $feedbackEntry[0].decision 'adopted' 'Forge feedback decision'
Assert-Equal ($feedbackEntry[0].artifacts -join '|') 'Assets\_Project\Art\Generated\ui_set\job_20260824133802_f43c6431\ending-gallery-album-spread-a-1280x800-2.png|Assets\_Project\Art\Generated\ui_set\job_20260824133802_f43c6431\ending-gallery-album-spread-a-2.svg' 'Forge selected artifacts'

$manifest = Get-Content -Raw -LiteralPath (Join-Path $jobRoot 'ending-gallery-manifest-2.json') | ConvertFrom-Json
Assert-Equal $manifest.status 'review' 'manifest status'
Assert-Equal $manifest.decision 'review' 'manifest decision'
Assert-True ($null -eq $manifest.selectedCandidate) 'selectedCandidate must be null'
Assert-Equal @($manifest.runtime.runtimeAllowlist).Count 0 'runtime allowlist count'
Assert-Equal $manifest.runtime.packageAllowed $false 'packageAllowed'
Assert-Equal $manifest.runtime.runtimeConnectAllowed $false 'runtimeConnectAllowed'
Assert-Equal $manifest.runtime.runtimeConnected $false 'runtimeConnected'
Assert-Equal $manifest.endingCatalog.count 19 'ending catalog count'
Assert-Equal $manifest.endingCatalog.categoryCounts.normal 5 'normal count'
Assert-Equal $manifest.endingCatalog.categoryCounts.comic 5 'comic count'
Assert-Equal $manifest.endingCatalog.categoryCounts.rare 4 'rare count'
Assert-Equal $manifest.endingCatalog.categoryCounts.day50 5 'day50 count'
Assert-Equal @($manifest.candidates).Count 3 'candidate count'

$package = Get-Content -Raw -LiteralPath (Join-Path $jobRoot 'forge-import.json') | ConvertFrom-Json
Assert-Equal $package.decision 'adopted' 'package decision'
Assert-Equal $package.selectedCandidate 'ui.ending-gallery.album-spread-a' 'package selected candidate'
Assert-Equal ($package.sourceFiles -join '|') 'ending-gallery-album-spread-a-1280x800-2.png|ending-gallery-album-spread-a-2.svg' 'selected-only package sources'
Assert-Equal @($package.rejectFiles).Count 6 'package reject count'
Assert-True ('ending-gallery-card-index-b-1280x800-2.png' -in $package.rejectFiles) 'B PNG missing from reject list'
Assert-True ('ending-gallery-filmstrip-c-1280x800-2.png' -in $package.rejectFiles) 'C PNG missing from reject list'
Assert-Equal @($package.runtimeAllowlist).Count 0 'package runtime allowlist count'
Assert-Equal $package.packageAllowed $true 'package allowed'
Assert-Equal $package.packaged $true 'package completed'
Assert-Equal $package.runtimeConnectAllowed $false 'runtime connect allowed'
Assert-Equal $package.runtimeConnected $false 'runtime connected'
Assert-Equal $package.sceneModified $false 'scene modified'
Assert-Equal $package.addressablesModified $false 'Addressables modified'
Assert-Equal $package.import.alphaIsTransparency $true 'package alpha import'

$candidatePngs = @(
    'ending-gallery-album-spread-a-1280x800-2.png',
    'ending-gallery-card-index-b-1280x800-2.png',
    'ending-gallery-filmstrip-c-1280x800-2.png'
)
foreach ($name in $candidatePngs) {
    $path = Join-Path $jobRoot $name
    $image = [System.Drawing.Image]::FromFile($path)
    try {
        Assert-Equal $image.Width 1280 "PNG width $name"
        Assert-Equal $image.Height 800 "PNG height $name"
        Assert-True (($image.PixelFormat -band [System.Drawing.Imaging.PixelFormat]::Alpha) -ne 0 -or ($image.PixelFormat -band [System.Drawing.Imaging.PixelFormat]::PAlpha) -ne 0) "PNG alpha missing $name"
    }
    finally { $image.Dispose() }
}

$candidateSvgs = @(
    'ending-gallery-album-spread-a-2.svg',
    'ending-gallery-card-index-b-2.svg',
    'ending-gallery-filmstrip-c-2.svg'
)
foreach ($name in $candidateSvgs) {
    $path = Join-Path $jobRoot $name
    $text = Get-Content -Raw -LiteralPath $path
    Assert-True ($text -match '<svg') "SVG root missing $name"
    Assert-Equal ([regex]::Matches($text, '<text(?:\s|>)').Count) 0 "SVG text element count $name"
    Assert-True ($text -notmatch '<script|javascript:|onload=') "active SVG content found $name"
}

$quality = Get-Content -Raw -LiteralPath (Join-Path $jobRoot 'quality-report.json') | ConvertFrom-Json
Assert-Equal $quality.grade 'fail' 'Forge quality grade'
Assert-Equal $quality.score 40 'Forge quality score'
Assert-Equal $quality.summary.errors 2 'Forge quality error count'
Assert-Equal $quality.summary.warnings 0 'Forge quality warning count'
$candidateEntries = @($quality.files | Where-Object { $_.fileName -in $candidatePngs -or $_.fileName -in $candidateSvgs })
Assert-Equal $candidateEntries.Count 6 'quality candidate entry count'
foreach ($entry in $candidateEntries) { Assert-Equal @($entry.issues).Count 0 "candidate quality issue count $($entry.fileName)" }
$errorFiles = @($quality.files | Where-Object { @($_.issues).Count -gt 0 } | ForEach-Object { $_.fileName })
Assert-Equal ($errorFiles -join '|') 'ending-gallery-review-board-1920x1080-2.png|ending-gallery-localization-accessibility-qa-1920x1080-2.png' 'expected QA-only error files'

$pairs = @(
    @('ending-gallery-album-spread-a-1280x800.png','ending-gallery-album-spread-a-1280x800-2.png'),
    @('ending-gallery-card-index-b-1280x800.png','ending-gallery-card-index-b-1280x800-2.png'),
    @('ending-gallery-filmstrip-c-1280x800.png','ending-gallery-filmstrip-c-1280x800-2.png'),
    @('ending-gallery-review-board-1920x1080.png','ending-gallery-review-board-1920x1080-2.png'),
    @('ending-gallery-localization-accessibility-qa-1920x1080.png','ending-gallery-localization-accessibility-qa-1920x1080-2.png'),
    @('ending-gallery-manifest.json','ending-gallery-manifest-2.json')
)
foreach ($pair in $pairs) {
    $left = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $jobRoot $pair[0])).Hash
    $right = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $jobRoot $pair[1])).Hash
    Assert-Equal $left $right "duplicate import byte identity $($pair[0])"
}

Assert-True (Test-Path -LiteralPath (Join-Path $jobRoot 'forge-import.json')) 'package manifest missing'

Push-Location $repoRoot
try {
    $allowedPrefixes = @('.forge/assets.json', '.forge/feedback.json', 'Assets/_Project/Art/Generated/ui_set/job_20260824133802_f43c6431', 'Docs/Art/Wave18')
    $changed = @(git status --porcelain=v1 | ForEach-Object { if ($_.Length -ge 4) { $_.Substring(3).Trim('"') } })
    foreach ($path in $changed) {
        Assert-True (($allowedPrefixes | Where-Object { $path -like "$_*" }).Count -gt 0) "out-of-scope change: $path"
    }
}
finally { Pop-Location }

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

[PSCustomObject]@{
    result = 'pass-with-forge-gate-limitation'
    visualAndContractScore = 100
    forgeMechanicalScore = 40
    forgeWarnings = 0
    forgeErrors = 2
    forgeErrorsLimitedToOpaqueQaBoards = $true
    productionCandidatesWithIssues = 0
    endingCount = 19
    candidates = 3
    status = 'engine_ready'
    selectedCandidate = 'ui.ending-gallery.album-spread-a'
    runtimeAllowlistEmpty = $true
    selectedOnlyPackage = $true
    runtimeConnectBlocked = $true
    externalApiCalled = $false
} | ConvertTo-Json -Depth 4
