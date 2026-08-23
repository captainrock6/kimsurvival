[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '671c4e9df8c144a22421e9eeab6693617aa2b3b1',

    [ValidateRange(5, 60)]
    [int]$MinimumSeconds = 6
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$executable = Join-Path $projectRoot (Join-Path (Join-Path 'work\ParallelQA' $RunId) 'WindowsBuild\KimSurvivalIsland.exe')
$preflightPath = Join-Path $evidenceRoot 'wave5-preflight.json'
$buildPath = Join-Path $evidenceRoot 'windows-development-build.json'
$addressBuildPath = Join-Path $evidenceRoot 'addressables-link-build-contract.json'
$cleanupPath = Join-Path $evidenceRoot 'addressables-generated-link-cleanup.json'
$contractsPath = Join-Path $evidenceRoot 'asset-contracts.json'
$visualPath = Join-Path $evidenceRoot 'wave5-current-visual-facts.json'
$steamPath = Join-Path $evidenceRoot 'steam-readiness.json'
$compilePath = Join-Path $evidenceRoot 'compile-result.txt'
$playerLog = Join-Path $evidenceRoot 'windows-player.log'
$linkRelative = 'Assets/AddressableAssetsData/link.xml'
$metaRelative = 'Assets/AddressableAssetsData/link.xml.meta'

if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-hidden-smoke.json')) {
    throw "Smoke evidence already exists; choose a new run ID instead of overwriting it: $evidenceRoot"
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

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
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
    if ($null -eq $Expected -or $null -eq $Actual) { return $false }
    $expectedAbsent = -not $Expected.linkExists -and -not $Expected.metaExists
    $actualAbsent = -not $Actual.linkExists -and -not $Actual.metaExists
    if ($expectedAbsent -or $actualAbsent) {
        return $expectedAbsent -and $actualAbsent -and
            [string]::IsNullOrEmpty([string]$Expected.linkSha256) -and [string]::IsNullOrEmpty([string]$Actual.linkSha256) -and
            [string]::IsNullOrEmpty([string]$Expected.metaSha256) -and [string]::IsNullOrEmpty([string]$Actual.metaSha256) -and
            [string]::IsNullOrEmpty([string]$Expected.metaGuid) -and [string]::IsNullOrEmpty([string]$Actual.metaGuid)
    }
    return $Expected.linkExists -and $Actual.linkExists -and $Expected.metaExists -and $Actual.metaExists -and
        $Expected.linkSha256 -eq $Actual.linkSha256 -and $Expected.metaSha256 -eq $Actual.metaSha256 -and
        $Expected.metaGuid -eq $Actual.metaGuid
}

$preflight = Read-Json $preflightPath
$build = Read-Json $buildPath
$addressBuild = Read-Json $addressBuildPath
$cleanup = Read-Json $cleanupPath
$contracts = Read-Json $contractsPath
$visual = Read-Json $visualPath
$steam = Read-Json $steamPath
$compileText = if (Test-Path -LiteralPath $compilePath) { Get-Content -LiteralPath $compilePath -Raw -Encoding UTF8 } else { '' }
$startedUtc = [DateTime]::UtcNow
$processId = 0
$earlyExit = $false
$exitCode = $null
$responding = $false
$aliveAtMinimum = $false
$launchError = ''

if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    $launchError = 'Windows executable is missing.'
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
$smokeStatus = if ($launchError -eq '' -and -not $earlyExit -and $aliveAtMinimum -and $responding) { 'PASS' } else { 'FAIL' }
$postAddress = Get-AddressSnapshot
$postStable = Test-AddressEqual $preflight.addressables $postAddress
$compileStatus = if ($compileText -match 'Result:\s+PASS' -and $compileText -match 'Compiler errors:\s+0' -and $compileText -match 'Compiler warnings:\s+0') { 'PASS' } else { 'FAIL' }
$buildStatus = if ($null -ne $build) { [string]$build.result } else { 'MISSING' }
$addressStatus = if ($null -ne $addressBuild) { [string]$addressBuild.overall } else { 'MISSING' }
$cleanupStatus = if ($null -ne $cleanup) { [string]$cleanup.overall } else { 'MISSING' }
$contractStatus = if ($null -ne $contracts) { [string]$contracts.overall } else { 'MISSING' }
$normalVisualStatus = if ($null -ne $visual) { [string]$visual.standardKoEnOverall } else { 'MISSING' }
$qpsStatus = if ($null -ne $visual) { [string]$visual.qpsLongOverall } else { 'MISSING' }
$steamStatus = if ($null -ne $steam) { [string]$steam.overall } else { 'MISSING' }
$backgroundCheck = if ($null -ne $contracts) { $contracts.checks | Where-Object id -eq 'build_reachability.background.island-camp' | Select-Object -First 1 } else { $null }
$backgroundStatus = if ($null -eq $backgroundCheck) { 'MISSING' } else { [string]$backgroundCheck.status }
$infrastructureGate = if ($compileStatus -eq 'PASS' -and $buildStatus -eq 'Succeeded' -and $addressStatus -eq 'PASS' -and $cleanupStatus -eq 'PASS' -and $postStable -and $smokeStatus -eq 'PASS') { 'PASS' } else { 'FAIL' }
$releaseOverall = if ($infrastructureGate -eq 'PASS' -and $contractStatus -eq 'PASS' -and $normalVisualStatus -eq 'PASS' -and $qpsStatus -eq 'PASS' -and $steamStatus -eq 'READY') { 'PASS' } else { 'FAIL' }

$smoke = [ordered]@{
    schemaVersion = 2
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
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'windows-hidden-smoke.json'), ($smoke | ConvertTo-Json -Depth 8) + [Environment]::NewLine, $utf8NoBom)
$smokeLines = @(
    'Wave 5 Windows hidden smoke'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Command: $($smoke.command)"
    "Result: $smokeStatus"
    "Resolution: 1280x800 windowed"
    "Required seconds: $MinimumSeconds"
    "Observed seconds: $($smoke.observedSeconds)"
    "Alive at minimum: $aliveAtMinimum"
    "Responding at minimum: $responding"
    "Executable SHA-256: $($smoke.executableSha256)"
)
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'windows-hidden-smoke.txt'), $smokeLines, $utf8NoBom)

$postContract = [ordered]@{
    schemaVersion = 2
    runId = $RunId
    baselineCommit = $BaselineCommit
    overall = if ($postStable) { 'PASS' } else { 'FAIL' }
    ownership = 'ADDRESSABLES_GENERATED_TEMPORARY'
    canonicalDurableState = 'ABSENT'
    preflight = if ($null -eq $preflight) { $null } else { $preflight.addressables }
    postSmoke = $postAddress
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json'), ($postContract | ConvertTo-Json -Depth 8) + [Environment]::NewLine, $utf8NoBom)

$summary = [ordered]@{
    schemaVersion = 2
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    qaBuildInfrastructureGate = $infrastructureGate
    releaseOverall = $releaseOverall
    unityCompile = $compileStatus
    addressablesLoadContract = if ($null -ne $contracts -and ($contracts.checks | Where-Object id -eq 'addressables.preflight_stability').status -eq 'PASS') { 'PASS' } else { 'FAIL' }
    addressablesBuildContract = $addressStatus
    addressablesGeneratedTemporaryCleanup = $cleanupStatus
    addressablesPostSmokeContract = if ($postStable) { 'PASS' } else { 'FAIL' }
    windowsDevelopmentBuild = $buildStatus
    hiddenSmoke = $smokeStatus
    normalKoEnVisualGate = $normalVisualStatus
    qpsLongVisualGate = $qpsStatus
    backgroundReachability = $backgroundStatus
    assetContracts = $contractStatus
    steamReadiness = $steamStatus
    physicalGamepad = 'UNVERIFIED'
    physicalGamepadReason = 'No physical-device actuation was performed; automated/code-path evidence cannot upgrade this status.'
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave5-release-summary.json'), ($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine, $utf8NoBom)
$summaryLines = @(
    'Wave 5 release evidence summary'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "QA/build infrastructure gate: $infrastructureGate"
    "Release overall: $releaseOverall"
    "Unity compile: $compileStatus"
    "Addressables load/build/post-smoke: $($summary.addressablesLoadContract)/$addressStatus/$($summary.addressablesPostSmokeContract)"
    "Generated temporary cleanup: $cleanupStatus"
    "Windows Development build: $buildStatus"
    "Hidden smoke: $smokeStatus"
    "Normal ko/en visual gate: $normalVisualStatus"
    "qps-long visual gate: $qpsStatus"
    "Background reachability: $backgroundStatus"
    "Steam readiness: $steamStatus"
    'Physical gamepad: UNVERIFIED'
)
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave5-release-summary.txt'), $summaryLines, $utf8NoBom)

Write-Output "QA_BUILD_INFRASTRUCTURE=$infrastructureGate"
Write-Output "RELEASE_OVERALL=$releaseOverall"
Write-Output "SMOKE=$smokeStatus"
Write-Output "ADDRESSABLES_POST_SMOKE=$($summary.addressablesPostSmokeContract)"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output "EVIDENCE=$evidenceRoot"
if ($infrastructureGate -ne 'PASS') { exit 1 }
