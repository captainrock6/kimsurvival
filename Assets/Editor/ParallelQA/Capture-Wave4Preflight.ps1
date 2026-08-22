[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = 'fed50669a37a66aec436c2174445e5107b74d57c'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$linkRelative = 'Assets/AddressableAssetsData/link.xml'
$metaRelative = 'Assets/AddressableAssetsData/link.xml.meta'
$visualSourceRelative = 'Assets/Editor/ParallelQA/Wave3VisualGate.cs'
$visualReportRelative = 'Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/wave3-visual-gate.txt'

if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a new run ID instead of overwriting it: $evidenceRoot"
}

function Get-Sha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return ''
    }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-MetaGuid([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return ''
    }
    $match = Select-String -LiteralPath $Path -Pattern '^guid:\s*([0-9a-fA-F]+)\s*$' | Select-Object -First 1
    if ($null -eq $match) {
        return ''
    }
    return $match.Matches[0].Groups[1].Value
}

function Get-AddressSnapshot {
    $link = Join-Path $projectRoot $linkRelative
    $meta = Join-Path $projectRoot $metaRelative
    $guid = Get-MetaGuid $meta
    return [ordered]@{
        observedUtc = [DateTime]::UtcNow.ToString('O')
        linkExists = Test-Path -LiteralPath $link -PathType Leaf
        linkBytes = if (Test-Path -LiteralPath $link -PathType Leaf) { (Get-Item -LiteralPath $link).Length } else { 0 }
        linkSha256 = Get-Sha256 $link
        metaExists = Test-Path -LiteralPath $meta -PathType Leaf
        metaBytes = if (Test-Path -LiteralPath $meta -PathType Leaf) { (Get-Item -LiteralPath $meta).Length } else { 0 }
        metaSha256 = Get-Sha256 $meta
        metaGuid = $guid
        assetDatabaseGuid = $guid
    }
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to resolve git HEAD.'
}
if ($head -ne $BaselineCommit) {
    throw "Preflight baseline mismatch. Expected $BaselineCommit, observed $head."
}

$branch = (& git -C $projectRoot branch --show-current).Trim()
$visualSource = Join-Path $projectRoot $visualSourceRelative
$visualReport = Join-Path $projectRoot $visualReportRelative
$visualLines = if (Test-Path -LiteralPath $visualReport -PathType Leaf) { Get-Content -LiteralPath $visualReport } else { @() }
$overallLine = $visualLines | Where-Object { $_ -match '^OVERALL:\s*' } | Select-Object -First 1
$thresholdLine = $visualLines | Where-Object { $_ -match '^Thresholds:\s*' } | Select-Object -First 1
$visualOverall = if ($null -eq $overallLine) { 'MISSING' } else { ($overallLine -replace '^OVERALL:\s*', '').Trim() }

New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
$report = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    capturedUtc = [DateTime]::UtcNow.ToString('O')
    command = $MyInvocation.Line
    baselineCommit = $BaselineCommit
    observedHead = $head
    branch = $branch
    addressables = Get-AddressSnapshot
    visualGate = [ordered]@{
        sourcePath = $visualSourceRelative
        sourceSha256 = Get-Sha256 $visualSource
        reportPath = $visualReportRelative
        reportSha256 = Get-Sha256 $visualReport
        overall = $visualOverall
        thresholds = if ($null -eq $thresholdLine) { '' } else { $thresholdLine }
    }
}

$jsonPath = Join-Path $evidenceRoot 'wave4-preflight.json'
$textPath = Join-Path $evidenceRoot 'wave4-preflight.txt'
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8NoBOM
@(
    'Wave 4 preflight snapshot'
    "Run ID: $RunId"
    "Captured UTC: $($report.capturedUtc)"
    "Command: $($report.command)"
    "Branch: $branch"
    "Baseline: $BaselineCommit"
    "Observed HEAD: $head"
    "Addressables link exists: $($report.addressables.linkExists)"
    "Addressables link SHA-256: $($report.addressables.linkSha256)"
    "Addressables meta exists: $($report.addressables.metaExists)"
    "Addressables meta SHA-256: $($report.addressables.metaSha256)"
    "Addressables GUID: $($report.addressables.metaGuid)"
    "Wave 3 visual source SHA-256: $($report.visualGate.sourceSha256)"
    "Wave 3 evidence: $visualReportRelative"
    "Wave 3 evidence SHA-256: $($report.visualGate.reportSha256)"
    "Wave 3 pixel-gate fact: $visualOverall"
    $report.visualGate.thresholds
) | Set-Content -LiteralPath $textPath -Encoding utf8NoBOM

Write-Output "PREFLIGHT=PASS"
Write-Output "EVIDENCE=$jsonPath"
Write-Output "VISUAL_PIXEL_GATE=$visualOverall"
