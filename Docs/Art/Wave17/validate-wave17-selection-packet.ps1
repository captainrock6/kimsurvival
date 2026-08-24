$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Push-Location $repoRoot

$failures = New-Object System.Collections.Generic.List[string]
function Assert-Equal {
    param($Actual, $Expected, [string]$Label)
    if ($Actual -ne $Expected) { $failures.Add("$Label expected '$Expected' but got '$Actual'") }
}
function Assert-True {
    param([bool]$Condition, [string]$Label)
    if (-not $Condition) { $failures.Add($Label) }
}
function Get-LowerHash {
    param([string]$Path)
    (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}
function Assert-ArrayEqual {
    param($Actual, $Expected, [string]$Label)
    Assert-Equal (@($Actual) -join '|') (@($Expected) -join '|') $Label
}

try {
    $immutableFiles = @{
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-atlas.png' = 'd294fe9273a8b1794aa558215e051d1bfb30b185b85d945e4ea0b184bcf5c7f8'
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-readability-32-64.png' = '9d139f3765fa0308e4d06ab52d321bbfd6d13c15330f392083531a975a8af8a4'
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-review-board-1920x1080.png' = 'db3fb8fa8e35916a696e3c18bd5a8bad981a424d00c46c326b04d4e8b28a1414'
        'Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-route-signature-a-1280x800.png' = '82c3288f78849d28848f8974d6972d6b2f1aa9f952622f63513c0f0ade1a9482'
        'Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-review-board-1920x1080.png' = '594004405f0e590cd9d0b54311919257b905931924f166315b1dd7f5bad5ce3a'
        'Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-triptych-a-1280x800.png' = 'c5b88ff152236d6a52c8157259dd1bac013c9c15f6736ffef42fdfca02a8a65d'
        'Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-review-board-1920x1080.png' = '54f36cae77dd5e3e59f35c0bc97806a905e189a1833ddc9048b75c5cbcd0c9cf'
        'Docs/Art/Wave17/wave17-hazard-escape-ending-selection-board.png' = 'd79c47b16c9a646cafb9c17040a5d4755ddc485cc8bbd821b00a63f5be20dfca'
    }
    foreach ($entry in $immutableFiles.GetEnumerator()) {
        Assert-True (Test-Path -LiteralPath $entry.Key) "missing immutable file: $($entry.Key)"
        if (Test-Path -LiteralPath $entry.Key) {
            Assert-Equal (Get-LowerHash $entry.Key) $entry.Value "sha256 $($entry.Key)"
        }
    }

    $dimensions = @{
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-atlas.png' = @(1024, 768)
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-readability-32-64.png' = @(1024, 512)
        'Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-route-signature-a-1280x800.png' = @(1280, 800)
        'Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-triptych-a-1280x800.png' = @(1280, 800)
        'Docs/Art/Wave17/wave17-hazard-escape-ending-selection-board.png' = @(3200, 3000)
    }
    foreach ($entry in $dimensions.GetEnumerator()) {
        $image = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $entry.Key))
        try {
            Assert-Equal $image.Width $entry.Value[0] "width $($entry.Key)"
            Assert-Equal $image.Height $entry.Value[1] "height $($entry.Key)"
        }
        finally { $image.Dispose() }
    }

    $assetExpectations = @(
        [PSCustomObject]@{ AssetId='effect.survival-hazards'; JobId='job_20260823160305_ef04b0f3'; StableId='effect.survival-hazards.phase-silhouette-a'; Root='Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3'; Sources=@('hazard-phase-atlas.png','hazard-phase-atlas.svg','hazard-phase-manifest.json'); MaxSize=1024; SpriteMode='Multiple' },
        [PSCustomObject]@{ AssetId='ui.escape-project-progress'; JobId='job_20260823160324_1de3b748'; StableId='ui.escape-project-progress.route-signature-a'; Root='Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748'; Sources=@('escape-project-route-signature-a-1280x800.png','escape-project-route-signature-a.svg'); MaxSize=2048; SpriteMode='Single' },
        [PSCustomObject]@{ AssetId='ui.ending-comic'; JobId='job_20260823160342_eceb3933'; StableId='ui.ending-comic.triptych-a'; Root='Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933'; Sources=@('ending-comic-triptych-a-1280x800.png','ending-comic-triptych-a.svg'); MaxSize=2048; SpriteMode='Single' }
    )

    $assets = Get-Content -Raw -LiteralPath '.forge/assets.json' | ConvertFrom-Json
    $feedback = Get-Content -Raw -LiteralPath '.forge/feedback.json' | ConvertFrom-Json
    foreach ($expectation in $assetExpectations) {
        $asset = $assets.assets | Where-Object { $_.id -eq $expectation.AssetId }
        Assert-True ($null -ne $asset) "asset missing: $($expectation.AssetId)"
        if ($null -ne $asset) {
            Assert-Equal $asset.status 'engine_ready' "Forge status $($expectation.AssetId)"
            Assert-Equal $asset.currentJobId $expectation.JobId "Forge job $($expectation.AssetId)"
            Assert-Equal $asset.engine.kind 'unity' "engine kind $($expectation.AssetId)"
            Assert-True (-not [string]::IsNullOrWhiteSpace($asset.engine.manifest)) "engine manifest missing $($expectation.AssetId)"
        }

        $entry = $feedback.entries | Where-Object { $_.jobId -eq $expectation.JobId } | Select-Object -Last 1
        Assert-True ($null -ne $entry) "feedback missing: $($expectation.JobId)"
        if ($null -ne $entry) {
            Assert-Equal $entry.decision 'adopted' "feedback decision $($expectation.JobId)"
            Assert-ArrayEqual (@($entry.artifacts) | ForEach-Object { Split-Path -Leaf $_ }) $expectation.Sources "feedback artifacts $($expectation.JobId)"
        }

        $importPath = Join-Path $expectation.Root 'forge-import.json'
        $import = Get-Content -Raw -LiteralPath $importPath | ConvertFrom-Json
        Assert-ArrayEqual $import.sourceFiles $expectation.Sources "package sources $($expectation.JobId)"
        Assert-Equal $import.import.maxSize $expectation.MaxSize "package maxSize $($expectation.JobId)"
        Assert-Equal $import.import.spriteMode $expectation.SpriteMode "package spriteMode $($expectation.JobId)"
        Assert-Equal @($import.runtimeAllowlist).Count 0 "runtime allowlist $($expectation.JobId)"
        Assert-Equal $import.runtimeConnectAllowed $false "runtimeConnectAllowed $($expectation.JobId)"
        Assert-Equal $import.runtimeConnected $false "runtimeConnected $($expectation.JobId)"

        $quality = Get-Content -Raw -LiteralPath (Join-Path $expectation.Root 'quality-report.json') | ConvertFrom-Json
        Assert-Equal $quality.grade 'pass' "quality grade $($expectation.JobId)"
        Assert-Equal $quality.score 100 "quality score $($expectation.JobId)"
        Assert-Equal $quality.summary.warnings 0 "quality warnings $($expectation.JobId)"
        Assert-Equal $quality.summary.errors 0 "quality errors $($expectation.JobId)"
    }

    $hazardImport = Get-Content -Raw -LiteralPath 'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/forge-import.json' | ConvertFrom-Json
    Assert-Equal $hazardImport.sliceGrid.columns 4 'hazard slice columns'
    Assert-Equal $hazardImport.sliceGrid.rows 3 'hazard slice rows'
    Assert-Equal $hazardImport.sliceGrid.frameWidth 256 'hazard frame width'
    Assert-Equal $hazardImport.sliceGrid.frameHeight 256 'hazard frame height'
    Assert-Equal $hazardImport.sliceGrid.frameCount 12 'hazard frame count'

    $packet = Get-Content -Raw -LiteralPath 'Docs/Art/Wave17/wave17-selection-packet-manifest.json' | ConvertFrom-Json
    Assert-Equal $packet.reviewSnapshotGate.decision 'review' 'pre-decision packet snapshot'
    Assert-Equal $packet.selectionResolution.decision 'adopted_all' 'selection resolution'
    Assert-Equal @($packet.selectionResolution.selectedStableIds).Count 3 'resolved stable ID count'
    Assert-Equal @($packet.selectionResolution.runtimeAllowlist).Count 0 'resolved runtime allowlist'
    Assert-Equal $packet.selectionResolution.runtimeConnectAllowed $false 'resolved runtimeConnectAllowed'

    $adoption = Get-Content -Raw -LiteralPath 'Docs/Art/Wave17/wave17-adoption-record.json' | ConvertFrom-Json
    Assert-Equal @($adoption.candidates).Count 3 'adoption candidate count'
    foreach ($candidate in $adoption.candidates) {
        Assert-Equal $candidate.decision 'adopted' "adoption decision $($candidate.stableId)"
        Assert-Equal $candidate.forgeStatus 'engine_ready' "adoption Forge status $($candidate.stableId)"
    }
    Assert-Equal @($adoption.runtime.runtimeAllowlist).Count 0 'adoption runtime allowlist'
    Assert-Equal $adoption.runtime.runtimeConnected $false 'adoption runtime connected'

    $allowedChanges = @(
        '.forge/assets.json',
        '.forge/feedback.json',
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/job.json',
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/forge-import.json',
        'Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/job.json',
        'Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/forge-import.json',
        'Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/job.json',
        'Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/forge-import.json'
    )
    $changedPaths = @(git status --porcelain=v1 | ForEach-Object { if ($_.Length -ge 4) { $_.Substring(3).Trim('"') } })
    foreach ($changedPath in $changedPaths) {
        $inWave17 = $changedPath -like 'Docs/Art/Wave17*'
        Assert-True ($inWave17 -or ($allowedChanges -contains $changedPath)) "out-of-scope changed path: $changedPath"
    }

    if ($failures.Count -gt 0) {
        $failures | ForEach-Object { Write-Error $_ }
        exit 1
    }

    [PSCustomObject]@{
        result = 'pass'
        score = 100
        warnings = 0
        errors = 0
        adoptedCandidates = 3
        forgeStatusAllEngineReady = $true
        selectedOnlyPackages = $true
        reviewBoardsExcludedFromPackages = $true
        sourceHashesUnchanged = $true
        runtimeAllowlistAllEmpty = $true
        runtimeConnectAllowedAllFalse = $true
        runtimeConnectedAllFalse = $true
        changedPathsWithinScope = $true
    } | ConvertTo-Json -Depth 4
}
finally {
    Pop-Location
}
