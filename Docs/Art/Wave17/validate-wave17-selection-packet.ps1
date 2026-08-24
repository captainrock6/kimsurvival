$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Push-Location $repoRoot

$failures = New-Object System.Collections.Generic.List[string]
function Assert-Equal {
    param($Actual, $Expected, [string]$Label)
    if ($Actual -ne $Expected) {
        $failures.Add("$Label expected '$Expected' but got '$Actual'")
    }
}
function Assert-True {
    param([bool]$Condition, [string]$Label)
    if (-not $Condition) { $failures.Add($Label) }
}

function Get-LowerHash {
    param([string]$Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

try {
    $expectedFiles = @{
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-atlas.png' = 'd294fe9273a8b1794aa558215e051d1bfb30b185b85d945e4ea0b184bcf5c7f8'
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-readability-32-64.png' = '9d139f3765fa0308e4d06ab52d321bbfd6d13c15330f392083531a975a8af8a4'
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-review-board-1920x1080.png' = 'db3fb8fa8e35916a696e3c18bd5a8bad981a424d00c46c326b04d4e8b28a1414'
        'Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-route-signature-a-1280x800.png' = '82c3288f78849d28848f8974d6972d6b2f1aa9f952622f63513c0f0ade1a9482'
        'Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-review-board-1920x1080.png' = '594004405f0e590cd9d0b54311919257b905931924f166315b1dd7f5bad5ce3a'
        'Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-triptych-a-1280x800.png' = 'c5b88ff152236d6a52c8157259dd1bac013c9c15f6736ffef42fdfca02a8a65d'
        'Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-review-board-1920x1080.png' = '54f36cae77dd5e3e59f35c0bc97806a905e189a1833ddc9048b75c5cbcd0c9cf'
        '.forge/assets.json' = 'b4b18c848c176eaa9640fe94a8781190529482ad296f7f2b1cbc1493bb2a1fd9'
        '.forge/feedback.json' = '628839169699e5c00923d3b5349b28ebe4b8b55b4aba7a8e3d7140117e5458c9'
        'Docs/Art/Wave17/wave17-hazard-escape-ending-selection-board.png' = 'd79c47b16c9a646cafb9c17040a5d4755ddc485cc8bbd821b00a63f5be20dfca'
    }

    foreach ($entry in $expectedFiles.GetEnumerator()) {
        Assert-True (Test-Path -LiteralPath $entry.Key) "missing file: $($entry.Key)"
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

    $assets = Get-Content -Raw -LiteralPath '.forge/assets.json' | ConvertFrom-Json
    $assetExpectations = @(
        @('effect.survival-hazards', 'job_20260823160305_ef04b0f3'),
        @('ui.escape-project-progress', 'job_20260823160324_1de3b748'),
        @('ui.ending-comic', 'job_20260823160342_eceb3933')
    )
    foreach ($expectation in $assetExpectations) {
        $asset = $assets.assets | Where-Object { $_.id -eq $expectation[0] }
        Assert-True ($null -ne $asset) "asset missing from Forge ledger: $($expectation[0])"
        if ($null -ne $asset) {
            Assert-Equal $asset.status 'review' "Forge status $($expectation[0])"
            Assert-Equal $asset.currentJobId $expectation[1] "Forge current job $($expectation[0])"
            Assert-Equal @($asset.engine.PSObject.Properties).Count 0 "Forge engine mapping count $($expectation[0])"
        }
    }

    $manifestPaths = @(
        'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-manifest.json',
        'Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-manifest.json',
        'Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-manifest.json'
    )
    foreach ($manifestPath in $manifestPaths) {
        $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
        Assert-Equal $manifest.status 'review' "manifest status $manifestPath"
        Assert-True ($null -eq $manifest.selectedCandidate) "manifest selectedCandidate must be null: $manifestPath"
        Assert-Equal @($manifest.runtime.allowlist).Count 0 "runtime allowlist count $manifestPath"
        Assert-Equal $manifest.runtime.packageAllowed $false "packageAllowed $manifestPath"
        Assert-Equal $manifest.runtime.runtimeConnectAllowed $false "runtimeConnectAllowed $manifestPath"
    }

    $packet = Get-Content -Raw -LiteralPath 'Docs/Art/Wave17/wave17-selection-packet-manifest.json' | ConvertFrom-Json
    Assert-Equal $packet.approvalGate.decision 'review' 'packet decision'
    Assert-True ($null -eq $packet.approvalGate.selectedCandidate) 'packet selectedCandidate must be null'
    Assert-Equal @($packet.approvalGate.runtimeAllowlist).Count 0 'packet runtime allowlist count'
    Assert-Equal $packet.approvalGate.packageAllowed $false 'packet packageAllowed'
    Assert-Equal $packet.approvalGate.runtimeConnectAllowed $false 'packet runtimeConnectAllowed'
    Assert-Equal @($packet.candidates).Count 3 'packet candidate count'
    foreach ($candidate in $packet.candidates) {
        Assert-Equal $candidate.status 'review' "packet candidate status $($candidate.stableId)"
        Assert-True ($null -eq $candidate.selectedCandidate) "packet candidate selectedCandidate $($candidate.stableId)"
        Assert-Equal @($candidate.runtimeAllowlist).Count 0 "packet candidate allowlist $($candidate.stableId)"
        Assert-Equal $candidate.packageAllowed $false "packet candidate packageAllowed $($candidate.stableId)"
        Assert-Equal $candidate.runtimeConnectAllowed $false "packet candidate runtimeConnectAllowed $($candidate.stableId)"
    }

    $changedPaths = @(
        git status --porcelain=v1 | ForEach-Object {
            if ($_.Length -ge 4) { $_.Substring(3).Replace('"', '') }
        }
    )
    foreach ($changedPath in $changedPaths) {
        Assert-True ($changedPath -like 'Docs/Art/Wave17*') "out-of-scope changed path: $changedPath"
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
        candidates = 3
        allReview = $true
        selectedCandidateAllNull = $true
        runtimeAllowlistAllEmpty = $true
        packageAllowedAllFalse = $true
        runtimeConnectAllowedAllFalse = $true
        sourceHashesUnchanged = $true
        forgeLedgerAndFeedbackUnchanged = $true
        changedPathsWithinScope = $true
    } | ConvertTo-Json -Depth 4
}
finally {
    Pop-Location
}
