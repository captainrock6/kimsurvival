[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = 'fed50669a37a66aec436c2174445e5107b74d57c',

    [ValidateRange(5, 60)]
    [int]$MinimumSeconds = 6
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$executable = Join-Path $projectRoot 'work\ParallelQA\StableWindowsBuild\KimSurvivalIsland.exe'
$preflightPath = Join-Path $evidenceRoot 'wave4-preflight.json'
$buildPath = Join-Path $evidenceRoot 'windows-development-build.json'
$addressBuildPath = Join-Path $evidenceRoot 'addressables-link-build-contract.json'
$contractsPath = Join-Path $evidenceRoot 'asset-contracts.json'
$steamPath = Join-Path $evidenceRoot 'steam-readiness.json'
$playerLog = Join-Path $evidenceRoot 'windows-player.log'
$linkRelative = 'Assets/AddressableAssetsData/link.xml'
$metaRelative = 'Assets/AddressableAssetsData/link.xml.meta'

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

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
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

function Test-AddressEqual($Expected, $Actual) {
    return $null -ne $Expected -and $null -ne $Actual -and
        $Expected.linkExists -and $Actual.linkExists -and
        $Expected.metaExists -and $Actual.metaExists -and
        $Expected.linkSha256 -eq $Actual.linkSha256 -and
        $Expected.metaSha256 -eq $Actual.metaSha256 -and
        $Expected.metaGuid -eq $Actual.metaGuid
}

New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$preflight = Read-Json $preflightPath
$build = Read-Json $buildPath
$addressBuild = Read-Json $addressBuildPath
$contracts = Read-Json $contractsPath
$steam = Read-Json $steamPath
$startedUtc = [DateTime]::UtcNow
$processId = 0
$earlyExit = $false
$exitCode = $null
$responding = $false
$aliveAtMinimum = $false
$launchError = ''
$expectedExecutable = [System.IO.Path]::GetFullPath($executable)
$reportedExecutable = if ($null -ne $build -and -not [string]::IsNullOrWhiteSpace([string]$build.executable)) {
    [System.IO.Path]::GetFullPath([string]$build.executable)
} else { '' }
$currentExecutableSha256 = Get-Sha256 $executable
$buildIdentityMatches = $null -ne $build -and
    [string]$build.runId -eq $RunId -and
    [string]$build.baselineCommit -eq $BaselineCommit -and
    $reportedExecutable.Equals($expectedExecutable, [System.StringComparison]::OrdinalIgnoreCase) -and
    -not [string]::IsNullOrWhiteSpace($currentExecutableSha256) -and
    [string]$build.executableSha256 -eq $currentExecutableSha256

if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    $launchError = 'Windows executable is missing.'
} elseif (-not $buildIdentityMatches) {
    $launchError = 'Stable Windows executable does not match this RunId, baseline, path, and build SHA-256 evidence.'
} else {
    try {
        $arguments = "-screen-width 1280 -screen-height 800 -screen-fullscreen 0 -logFile `"$playerLog`""
        $process = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
        $processId = $process.Id
        $watch = [Diagnostics.Stopwatch]::StartNew()
        while ($watch.Elapsed.TotalSeconds -lt $MinimumSeconds) {
            Start-Sleep -Milliseconds 250
            $process.Refresh()
            if ($process.HasExited) {
                $earlyExit = $true
                $exitCode = $process.ExitCode
                break
            }
        }
        $watch.Stop()
        $process.Refresh()
        if (-not $process.HasExited) {
            $aliveAtMinimum = $watch.Elapsed.TotalSeconds -ge $MinimumSeconds
            $responding = $process.Responding
            Stop-Process -Id $process.Id -Force
            $process.WaitForExit(5000) | Out-Null
        }
    } catch {
        $launchError = $_.Exception.Message
    }
}

$completedUtc = [DateTime]::UtcNow
$durationSeconds = ($completedUtc - $startedUtc).TotalSeconds
$smokeStatus = if ($launchError -eq '' -and -not $earlyExit -and $aliveAtMinimum) { 'PASS' } else { 'FAIL' }
$postAddress = Get-AddressSnapshot
$postStable = Test-AddressEqual $preflight.addressables $postAddress
$buildStatus = if ($null -ne $build) { [string]$build.result } else { 'MISSING' }
$addressStatus = if ($null -ne $addressBuild) { [string]$addressBuild.overall } else { 'MISSING' }
$contractStatus = if ($null -ne $contracts) { [string]$contracts.overall } else { 'MISSING' }
$steamStatus = if ($null -ne $steam) { [string]$steam.overall } else { 'MISSING' }
$overall = if ($smokeStatus -eq 'PASS' -and $buildStatus -eq 'Succeeded' -and $addressStatus -eq 'PASS' -and $postStable -and $contractStatus -eq 'PASS' -and $steamStatus -eq 'READY') { 'PASS' } else { 'FAIL' }

$smoke = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    command = $MyInvocation.Line
    startedUtc = $startedUtc.ToString('O')
    completedUtc = $completedUtc.ToString('O')
    minimumSeconds = $MinimumSeconds
    observedSeconds = [Math]::Round($durationSeconds, 3)
    executable = $executable
    executableExists = Test-Path -LiteralPath $executable -PathType Leaf
    executableSha256 = Get-Sha256 $executable
    stableExecutablePath = $true
    buildIdentityMatches = $buildIdentityMatches
    processId = $processId
    windowStyle = 'Hidden'
    resolution = '1280x800 windowed'
    aliveAtMinimum = $aliveAtMinimum
    respondingAtMinimum = $responding
    earlyExit = $earlyExit
    exitCode = $exitCode
    launchError = $launchError
    result = $smokeStatus
}
$smoke | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'windows-hidden-smoke.json') -Encoding utf8NoBOM
@(
    'Wave 4 Windows hidden smoke'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Command: $($smoke.command)"
    "Result: $smokeStatus"
    "Executable: $executable"
    "Executable SHA-256: $($smoke.executableSha256)"
    "Stable executable path: $($smoke.stableExecutablePath)"
    "Build identity matches: $buildIdentityMatches"
    "PID: $processId"
    "Hidden: True"
    "Resolution: 1280x800 windowed"
    "Required seconds: $MinimumSeconds"
    "Observed seconds: $($smoke.observedSeconds)"
    "Alive at minimum: $aliveAtMinimum"
    "Responding at minimum: $responding"
    "Early exit: $earlyExit"
    "Exit code: $exitCode"
    "Launch error: $launchError"
) | Set-Content -LiteralPath (Join-Path $evidenceRoot 'windows-hidden-smoke.txt') -Encoding utf8NoBOM

$postContract = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    overall = if ($postStable) { 'PASS' } else { 'FAIL' }
    preflight = if ($null -eq $preflight) { $null } else { $preflight.addressables }
    postSmoke = $postAddress
}
$postContract | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json') -Encoding utf8NoBOM

$summary = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    overall = $overall
    assetContracts = $contractStatus
    windowsDevelopmentBuild = $buildStatus
    hiddenSmoke = $smokeStatus
    addressablesLinkBuildContract = $addressStatus
    addressablesLinkPostSmokeContract = if ($postStable) { 'PASS' } else { 'FAIL' }
    visualPixelGateBaseline = if ($null -eq $preflight) { 'MISSING' } else { [string]$preflight.visualGate.overall }
    steamReadiness = $steamStatus
    physicalGamepad = 'UNVERIFIED'
    physicalGamepadReason = 'No human physical-device actuation was performed; automated/code-path evidence cannot upgrade this status.'
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'wave4-release-summary.json') -Encoding utf8NoBOM
@(
    'Wave 4 release evidence summary'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Overall: $overall"
    "Asset contracts: $contractStatus"
    "Windows Development build: $buildStatus"
    "Hidden smoke: $smokeStatus"
    "Addressables build contract: $addressStatus"
    "Addressables post-smoke contract: $($summary.addressablesLinkPostSmokeContract)"
    "Wave 3 visual pixel gate baseline: $($summary.visualPixelGateBaseline)"
    "Steam readiness: $steamStatus"
    'Physical gamepad: UNVERIFIED'
) | Set-Content -LiteralPath (Join-Path $evidenceRoot 'wave4-release-summary.txt') -Encoding utf8NoBOM

Write-Output "SMOKE=$smokeStatus"
Write-Output "OVERALL=$overall"
Write-Output "PHYSICAL_GAMEPAD=UNVERIFIED"
Write-Output "EVIDENCE=$evidenceRoot"
if ($smokeStatus -ne 'PASS' -or -not $postStable) { exit 1 }
