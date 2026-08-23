[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '671c4e9df8c144a22421e9eeab6693617aa2b3b1'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$linkRelative = 'Assets/AddressableAssetsData/link.xml'
$metaRelative = 'Assets/AddressableAssetsData/link.xml.meta'
$ignoreRelative = 'Assets/AddressableAssetsData/.gitignore'
$visualSourceRelative = 'Assets/Editor/ParallelQA/Wave3VisualGate.cs'
$settingsRelative = 'Assets/AddressableAssetsData/AddressableAssetSettings.asset'

if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a new run ID instead of overwriting it: $evidenceRoot"
}

function Get-Sha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-MetaGuid([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    $match = Select-String -LiteralPath $Path -Pattern '^guid:\s*([0-9a-fA-F]+)\s*$' | Select-Object -First 1
    if ($null -eq $match) { return '' }
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
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve git HEAD.' }
if ($head -ne $BaselineCommit) { throw "Preflight baseline mismatch. Expected $BaselineCommit, observed $head." }

$branch = (& git -C $projectRoot branch --show-current).Trim()
$tracked = @(& git -C $projectRoot ls-files -- $linkRelative $metaRelative)
$ignored = @(& git -C $projectRoot check-ignore --no-index -- $linkRelative $metaRelative)
$address = Get-AddressSnapshot
$ownershipPass = -not $address.linkExists -and -not $address.metaExists -and $tracked.Count -eq 0 -and $ignored.Count -eq 2
$packageProcessor = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library\PackageCache') -Filter 'AddressablesPlayerBuildProcessor.cs' -Recurse -ErrorAction Stop |
    Where-Object { $_.FullName -match 'com\.unity\.addressables@' } | Select-Object -First 1
if ($null -eq $packageProcessor) { throw 'Installed AddressablesPlayerBuildProcessor.cs was not found.' }

New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
$report = [ordered]@{
    schemaVersion = 2
    runId = $RunId
    capturedUtc = [DateTime]::UtcNow.ToString('O')
    command = $MyInvocation.Line
    baselineCommit = $BaselineCommit
    observedHead = $head
    branch = $branch
    ownershipOverall = if ($ownershipPass) { 'PASS' } else { 'FAIL' }
    ownership = [ordered]@{
        policy = 'ADDRESSABLES_GENERATED_TEMPORARY'
        canonicalDurableState = 'ABSENT'
        trackedPaths = $tracked
        ignoredPaths = $ignored
        ignoreFile = $ignoreRelative
        ignoreFileSha256 = Get-Sha256 (Join-Path $projectRoot $ignoreRelative)
    }
    addressables = $address
    officialPackageImplementation = [ordered]@{
        path = $packageProcessor.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
        sha256 = Get-Sha256 $packageProcessor.FullName
        initializeOnLoadMethod = 'CleanTemporaryPlayerBuildData -> RemovePlayerBuildLinkXML'
        playerBuildMethod = 'PrepareForPlayerbuild copies Library AddressablesLink/link.xml to AddressableAssetSettings.ConfigFolder'
    }
    addressableSettings = [ordered]@{
        path = $settingsRelative
        sha256 = Get-Sha256 (Join-Path $projectRoot $settingsRelative)
    }
    visualGate = [ordered]@{
        sourcePath = $visualSourceRelative
        sourceSha256 = Get-Sha256 (Join-Path $projectRoot $visualSourceRelative)
        reportPath = ''
        reportSha256 = ''
        overall = 'PENDING_FRESH_RUN'
        thresholds = 'source hash captured before fresh 671c4e9 Play Mode run'
    }
}

$jsonPath = Join-Path $evidenceRoot 'wave5-preflight.json'
$textPath = Join-Path $evidenceRoot 'wave5-preflight.txt'
[System.IO.File]::WriteAllText($jsonPath, ($report | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)
$textLines = @(
    'Wave 5 Addressables ownership preflight'
    "Run ID: $RunId"
    "Captured UTC: $($report.capturedUtc)"
    "Command: $($report.command)"
    "Branch: $branch"
    "Baseline: $BaselineCommit"
    "Observed HEAD: $head"
    "Ownership: $($report.ownershipOverall)"
    "Canonical durable state: $($report.ownership.canonicalDurableState)"
    "Tracked temporary paths: $($tracked -join ' | ')"
    "Ignored temporary paths: $($ignored -join ' | ')"
    "Link exists: $($address.linkExists)"
    "Link SHA-256: $($address.linkSha256)"
    "Meta exists: $($address.metaExists)"
    "Meta SHA-256: $($address.metaSha256)"
    "Meta GUID: $($address.metaGuid)"
    "Installed processor: $($report.officialPackageImplementation.path)"
    "Installed processor SHA-256: $($report.officialPackageImplementation.sha256)"
    "Wave3VisualGate source SHA-256: $($report.visualGate.sourceSha256)"
)
[System.IO.File]::WriteAllLines($textPath, $textLines, $utf8NoBom)

Write-Output "PREFLIGHT=$($report.ownershipOverall)"
Write-Output "CANONICAL_ADDRESSABLES_LINK=ABSENT"
Write-Output "EVIDENCE=$jsonPath"
if (-not $ownershipPass) { exit 1 }
