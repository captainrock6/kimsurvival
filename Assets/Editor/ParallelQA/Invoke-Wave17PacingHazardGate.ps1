[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = 'a5403173f299abc71ed4724bdaaf30c31ce8cc94',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(6, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$wave16Entry = Join-Path $PSScriptRoot 'Invoke-Wave16HazardEndingGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$redBaseline = 'a5403173f299abc71ed4724bdaaf30c31ce8cc94'
$frozenWave16Failures = @(
    'W16-H01.hazard_phase_catalog',
    'W16-H02.daily_stack_budget',
    'W16-H03.atomic_idempotent_resolution',
    'W16-E01.five_escape_catalog',
    'W16-E02.escape_axis_separation',
    'W16-E03.smoke_radio_playable',
    'W16-E04.raft_flare_beacon_data',
    'W16-O01.snapshot_and_private_log',
    'W16-N01.ending_catalog_19',
    'W16-N02.deterministic_single_ending',
    'W16-N03.terminal_priority',
    'W16-L01.ko_en_qps_contract',
    'W16-P01.live_hazard_lifecycle',
    'W16-P02.live_escape_paths',
    'W16-P03.three_panel_comic',
    'W16-P04.ko_en_qps_1280',
    'W16-P05.keyboard_gamepad_state_parity'
)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $wave16Entry -PathType Leaf)) {
    throw "Wave 16 prerequisite entry point is missing: $wave16Entry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 17 baseline mismatch. Expected $BaselineCommit, observed $head"
}

New-Item -ItemType Directory -Path $workRoot -Force | Out-Null
$env:KIM_PARALLEL_QA_RUN_ID = $RunId
$env:KIM_PARALLEL_QA_BASELINE = $BaselineCommit

function Quote-Argument([string]$Value) {
    if ($Value -match '[\s"]') { return '"' + $Value.Replace('"', '\"') + '"' }
    return $Value
}

function Invoke-HiddenProcess(
    [string]$Name,
    [string]$Executable,
    [string[]]$Arguments,
    [string]$StandardOutput,
    [string]$StandardError
) {
    $argumentLine = [string]::Join(' ', @($Arguments | ForEach-Object { Quote-Argument $_ }))
    $started = [DateTime]::UtcNow
    $parameters = @{
        FilePath = $Executable
        ArgumentList = $argumentLine
        WindowStyle = 'Hidden'
        Wait = $true
        PassThru = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($StandardOutput)) { $parameters.RedirectStandardOutput = $StandardOutput }
    if (-not [string]::IsNullOrWhiteSpace($StandardError)) { $parameters.RedirectStandardError = $StandardError }
    $process = Start-Process @parameters
    return [ordered]@{
        name = $Name
        startedUtc = $started.ToString('O')
        completedUtc = [DateTime]::UtcNow.ToString('O')
        exitCode = $process.ExitCode
        command = (Quote-Argument $Executable) + ' ' + $argumentLine
        standardOutput = $StandardOutput
        standardError = $StandardError
    }
}

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Test-Utf8NoBom([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    return $bytes.Length -lt 3 -or -not ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
}

function Compare-ExactSet([object[]]$Observed, [string[]]$Expected) {
    $observedValues = @($Observed | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $expectedValues = @($Expected | Sort-Object -Unique)
    return $observedValues.Count -eq $expectedValues.Count -and
        [string]::Join('|', $observedValues) -eq [string]::Join('|', $expectedValues)
}

$runStarted = [DateTime]::UtcNow
$shellEdition = if ([string]::IsNullOrWhiteSpace([string]$PSVersionTable.PSEdition)) { 'Desktop' } else { [string]$PSVersionTable.PSEdition }
$shellVersion = [string]$PSVersionTable.PSVersion
$shellExecutable = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName

# Wave 16 invokes the complete Wave 15 prerequisite. Its non-zero exit on the
# exact a540317 RED baseline is product evidence, not an infrastructure result.
$wave16Arguments = @(
    '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $wave16Entry,
    '-RunId', $RunId, '-BaselineCommit', $BaselineCommit, '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$wave16Stage = Invoke-HiddenProcess 'wave16-frozen-foundation-and-wave15-green' $shellExecutable $wave16Arguments `
    (Join-Path $workRoot 'wave17-wave16-stdout.log') (Join-Path $workRoot 'wave17-wave16-stderr.log')

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}

$editArguments = @(
    '-batchmode', '-nographics', '-quit', '-projectPath', $projectRoot,
    '-executeMethod', 'ParallelQA.Wave17PacingHazardRedFirstHardeningRunner.RunEditContracts',
    '-logFile', (Join-Path $workRoot 'unity-wave17-edit.log')
)
$editStage = Invoke-HiddenProcess 'wave17-pacing-hazard-edit' $UnityPath $editArguments `
    (Join-Path $workRoot 'wave17-edit-stdout.log') (Join-Path $workRoot 'wave17-edit-stderr.log')

$playArguments = @(
    '-batchmode', '-force-d3d11', '-projectPath', $projectRoot,
    '-executeMethod', 'ParallelQA.Wave17PacingHazardRedFirstHardeningRunner.RunPlayContracts',
    '-logFile', (Join-Path $workRoot 'unity-wave17-play.log')
)
$playStage = Invoke-HiddenProcess 'wave17-pacing-hazard-play' $UnityPath $playArguments `
    (Join-Path $workRoot 'wave17-play-stdout.log') (Join-Path $workRoot 'wave17-play-stderr.log')

$wave16Summary = Read-Json (Join-Path $evidenceRoot 'wave16-summary.json')
$wave15Summary = Read-Json (Join-Path $evidenceRoot 'wave15-summary.json')
$wave14Gate = Read-Json (Join-Path $evidenceRoot 'wave14-qps-global-layout-gate.json')
$wave12Summary = Read-Json (Join-Path $evidenceRoot 'wave12-summary.json')
$smoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$addressables = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$edit = Read-Json (Join-Path $evidenceRoot 'wave17-edit-contracts.json')
$play = Read-Json (Join-Path $evidenceRoot 'wave17-play-contracts.json')

# Unity's raw Development Player log contains local network interfaces and the
# Windows host name. Preserve it only under ignored work/ for local diagnosis;
# the durable smoke JSON is the privacy-safe release artifact.
$rawPlayerLog = Join-Path $evidenceRoot 'windows-player.log'
$quarantinedPlayerLog = Join-Path $workRoot 'windows-player.raw.log'
$playerLogPrivacy = 'NOT_GENERATED'
if (Test-Path -LiteralPath $rawPlayerLog -PathType Leaf) {
    $resolvedEvidence = [System.IO.Path]::GetFullPath($evidenceRoot).TrimEnd('\') + '\'
    $resolvedWork = [System.IO.Path]::GetFullPath($workRoot).TrimEnd('\') + '\'
    $resolvedRaw = [System.IO.Path]::GetFullPath($rawPlayerLog)
    $resolvedQuarantine = [System.IO.Path]::GetFullPath($quarantinedPlayerLog)
    if (-not $resolvedRaw.StartsWith($resolvedEvidence, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not $resolvedQuarantine.StartsWith($resolvedWork, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to quarantine a Windows Player log outside the exact Wave 17 evidence/work roots.'
    }
    Move-Item -LiteralPath $resolvedRaw -Destination $resolvedQuarantine -Force
    $playerLogPrivacy = 'PASS_RAW_LOG_QUARANTINED_TO_IGNORED_WORK'
}

$compileLogPath = Join-Path $workRoot 'unity-wave17-edit.log'
$compileText = if (Test-Path -LiteralPath $compileLogPath) { Get-Content -LiteralPath $compileLogPath -Raw } else { '' }
$compileErrors = @([regex]::Matches($compileText, '(?im)\berror\s+CS\d+\b')).Count
$compileWarnings = @([regex]::Matches($compileText, '(?im)\bwarning\s+CS\d+\b')).Count

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($null -eq $wave16Summary -or $wave16Summary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 16 prerequisite summary is missing or infrastructure FAIL')
}
if ($BaselineCommit -eq $redBaseline) {
    $observedFrozen = if ($null -eq $wave16Summary) { @() } else { @($wave16Summary.productFailures | ForEach-Object { [string]$_.id }) }
    if (-not (Compare-ExactSet $observedFrozen $frozenWave16Failures)) {
        $infrastructureFailures.Add("exact a540317 baseline did not reproduce the frozen 17 Wave 16 product failure IDs; observed=$([string]::Join(',', $observedFrozen))")
    }
    if ($null -ne $wave16Summary -and @($wave16Summary.expectedGapIds).Count -ne 0) {
        $infrastructureFailures.Add('a540317 Wave 16 foundation must be recorded as 17 product failures, not legacy EXPECTED_GAP')
    }
} elseif ($null -eq $wave16Summary -or $wave16Summary.overall -ne 'GREEN' -or $wave16Summary.productOverall -ne 'PASS') {
    $infrastructureFailures.Add('post-implementation baseline must turn the frozen Wave 16 foundation fully GREEN')
}
if ($null -eq $wave15Summary -or $wave15Summary.overall -ne 'GREEN' -or
    $wave15Summary.productOverall -ne 'PASS' -or $wave15Summary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 15 campaign/map prerequisite did not remain GREEN')
}
if ($null -eq $wave14Gate -or $wave14Gate.productOverall -ne 'PASS' -or $wave14Gate.infrastructureOverall -ne 'PASS' -or
    [int]$wave14Gate.targetCount -ne 10 -or [int]$wave14Gate.passedTargets -ne 10) {
    $infrastructureFailures.Add('fresh qps-long global layout lock is not 10/10 PASS')
}
if ($null -eq $wave12Summary -or $wave12Summary.infrastructureOverall -ne 'PASS' -or
    $wave12Summary.compile -notmatch '^PASS' -or $wave12Summary.windowsDevelopmentBuild -ne 'PASS' -or
    $wave12Summary.hiddenSmoke -ne 'PASS' -or $wave12Summary.addressables -notmatch '^PASS') {
    $infrastructureFailures.Add('compile/Windows build/hidden smoke/Addressables regression lock did not remain PASS')
}
if ($null -eq $smoke -or $smoke.result -ne 'PASS' -or [double]$smoke.observedSeconds -lt $MinimumSmokeSeconds -or
    -not [bool]$smoke.aliveAtMinimum -or -not [bool]$smoke.respondingAtMinimum) {
    $infrastructureFailures.Add("hidden Windows smoke did not remain alive/responding for at least $MinimumSmokeSeconds seconds")
}
if ($null -eq $addressables -or $addressables.overall -ne 'PASS') {
    $infrastructureFailures.Add('Addressables post-smoke ownership/stability contract did not remain PASS')
}
if ($compileErrors -ne 0) {
    $infrastructureFailures.Add("Wave 17 Unity compilation reported $compileErrors C# error(s)")
}
foreach ($report in @($edit, $play)) {
    if ($null -eq $report -or $report.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add('a Wave 17 machine-readable report is missing or infrastructure FAIL')
    }
}

$allChecks = @()
foreach ($report in @($edit, $play)) { if ($null -ne $report) { $allChecks += @($report.checks) } }
$expectedGaps = @($allChecks | Where-Object { $_.status -eq 'EXPECTED_GAP' -and $_.classification -eq 'PRODUCT_EXPECTED_GAP' })
$productFailures = @($allChecks | Where-Object { $_.status -eq 'FAIL' })
$infrastructureChecks = @($allChecks | Where-Object { $_.status -eq 'INFRA_FAIL' })
$unverified = @($allChecks | Where-Object { $_.status -eq 'UNVERIFIED' })
if ($infrastructureChecks.Count -gt 0) {
    $infrastructureFailures.Add("Wave 17 reports contain $($infrastructureChecks.Count) infrastructure check failure(s)")
}
if ($BaselineCommit -eq $redBaseline -and $expectedGaps.Count -eq 0) {
    $infrastructureFailures.Add('the exact a540317 RED baseline unexpectedly produced zero Wave 17 product gaps')
}
if ($BaselineCommit -ne $redBaseline -and $expectedGaps.Count -gt 0) {
    $infrastructureFailures.Add('EXPECTED_GAP classification is only permitted on the exact a540317 RED baseline')
}

$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($productFailures.Count -gt 0) { 'FAIL' } elseif ($expectedGaps.Count -gt 0) { 'RED_EXPECTED_GAP' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'RED_EXPECTED_GAP') { 'RED' } else { 'GREEN' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave17PacingHazardGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 17 pacing, hazard, escape, ending RED-first hardening gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    powershell = [ordered]@{
        edition = $shellEdition
        version = $shellVersion
        executable = $shellExecutable
        compatibility = 'Windows PowerShell 5.1 and PowerShell 7; UTF-8 without BOM evidence writes'
    }
    executionPolicy = 'All Unity Editor/build and Windows Player stages inherit this outside-sandbox process; no -noUpm.'
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    frozenWave16Baseline = [ordered]@{
        expectedCount = 17
        expectedIds = $frozenWave16Failures
        observedIds = if ($null -eq $wave16Summary) { @() } else { @($wave16Summary.productFailures | ForEach-Object { [string]$_.id }) }
        result = if ($null -ne $wave16Summary -and (Compare-ExactSet @($wave16Summary.productFailures | ForEach-Object { [string]$_.id }) $frozenWave16Failures)) { 'PASS 17/17' } else { 'FAIL' }
    }
    currentGreenLocks = [ordered]@{
        wave15CampaignMap = if ($null -ne $wave15Summary) { [string]$wave15Summary.overall } else { 'MISSING' }
        qpsLong = if ($null -ne $wave14Gate) { "$($wave14Gate.passedTargets)/$($wave14Gate.targetCount) $($wave14Gate.productOverall)" } else { 'MISSING' }
        compile = if ($compileErrors -eq 0 -and $null -ne $edit) { "PASS 0 errors / $compileWarnings warnings" } else { "FAIL $compileErrors errors / $compileWarnings warnings" }
        windowsDevelopmentBuild = if ($null -ne $wave12Summary) { [string]$wave12Summary.windowsDevelopmentBuild } else { 'MISSING' }
        hiddenSmoke = if ($null -ne $smoke) { "$($smoke.result) $($smoke.observedSeconds)s (minimum $MinimumSmokeSeconds)" } else { 'MISSING' }
        addressables = if ($null -ne $addressables) { [string]$addressables.overall } else { 'MISSING' }
    }
    expectedGapIds = @($expectedGaps | ForEach-Object { [string]$_.id })
    expectedGaps = @($expectedGaps | ForEach-Object { [ordered]@{
        id = [string]$_.id
        severity = [string]$_.severity
        actual = [string]$_.actual
        reproduction = [string]$_.reproduction
        recommendedFiles = [string]$_.recommendedFiles
    } })
    productFailures = @($productFailures | ForEach-Object { [ordered]@{
        id = [string]$_.id
        classification = [string]$_.classification
        severity = [string]$_.severity
        actual = [string]$_.actual
        reproduction = [string]$_.reproduction
        recommendedFiles = [string]$_.recommendedFiles
    } })
    infrastructureFailures = $infrastructureFailures.ToArray()
    unverifiedIds = @($unverified | ForEach-Object { [string]$_.id } | Sort-Object -Unique)
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    steamReadyClaim = $false
    reviewOnlyArt = 'Three human-adoption candidates must remain unselected and absent from runtime/scene/Addressables references.'
    playerLogPrivacy = $playerLogPrivacy
    greenTransition = 'On a post-implementation baseline, fresh Wave 15 and Wave 16 must be GREEN, infrastructure locks PASS, and this gate must report zero EXPECTED_GAP/FAIL checks while review-only art remains unconnected until explicit human adoption.'
    exactRerun = $exactRerun
    stages = @($wave16Stage, $editStage, $playStage)
    exitCode = $exitCode
}

$jsonPath = Join-Path $evidenceRoot 'wave17-summary.json'
$txtPath = Join-Path $evidenceRoot 'wave17-summary.txt'
[System.IO.File]::WriteAllText($jsonPath, ($summary | ConvertTo-Json -Depth 18) + [Environment]::NewLine, $utf8NoBom)
[System.IO.File]::WriteAllLines($txtPath, @(
    'Wave 17 pacing, hazard, escape, ending RED-first hardening gate'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "PowerShell: $shellEdition $shellVersion"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Frozen Wave 16 failures: $($summary.frozenWave16Baseline.result)"
    "Wave 15/qps: $($summary.currentGreenLocks.wave15CampaignMap)/$($summary.currentGreenLocks.qpsLong)"
    "Compile/Build/Smoke/Addressables: $($summary.currentGreenLocks.compile)/$($summary.currentGreenLocks.windowsDevelopmentBuild)/$($summary.currentGreenLocks.hiddenSmoke)/$($summary.currentGreenLocks.addressables)"
    "Expected Wave 17 product gaps: $($expectedGaps.Count) [$([string]::Join(', ', @($summary.expectedGapIds)))]"
    "Unexpected product/adoption failures: $($productFailures.Count)"
    "Infrastructure failures: $($infrastructureFailures.Count)"
    'Physical gamepad: UNVERIFIED'
    'Steam: NOT_READY'
    "Exit code: $exitCode (0 GREEN, 2 RED_EXPECTED_GAP, 1 unexpected/infrastructure failure)"
    "Rerun: $exactRerun"
), $utf8NoBom)

$utf8Result = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    powershellEdition = $shellEdition
    powershellVersion = $shellVersion
    jsonUtf8NoBom = Test-Utf8NoBom $jsonPath
    textUtf8NoBom = Test-Utf8NoBom $txtPath
    overall = if ((Test-Utf8NoBom $jsonPath) -and (Test-Utf8NoBom $txtPath)) { 'PASS' } else { 'FAIL' }
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave17-powershell-compatibility.json'), ($utf8Result | ConvertTo-Json -Depth 6) + [Environment]::NewLine, $utf8NoBom)
if ($utf8Result.overall -ne 'PASS') {
    Write-Error 'Wave 17 evidence encoding compatibility failed.'
    exit 1
}

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "EXPECTED_GAPS=$($expectedGaps.Count)"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output 'STEAM=NOT_READY'
Write-Output "EVIDENCE=$evidenceRoot"
exit $exitCode
