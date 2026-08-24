[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = 'fac8545148e1422fc6258f57cab2205cbb4596a9',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(6, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'Wave 18 requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$wave17Entry = Join-Path $PSScriptRoot 'Invoke-Wave17PacingHazardGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$redBaseline = 'fac8545148e1422fc6258f57cab2205cbb4596a9'

$baselineFailureIds = @(
    'W17-T01.day_band_boundaries',
    'W17-T02.early_escape_no_hardlock',
    'W17-R01.six_region_primary_alternative',
    'W17-R02.seed_forecast_hazard_pity_determinism',
    'W17-R03.eligible_search_hint3_guarantee5',
    'W17-R04.minimum_three_completable_paths',
    'W17-H02.rolling_calm_and_major_recovery',
    'W17-H03.atomic_retry_loss_and_keypart_protection',
    'W17-E02.smoke_radio_natural_interaction_routes',
    'W17-E03.raft_flare_beacon_data_only',
    'W17-O01.snapshot_and_private_log',
    'W17-N02.priority_tiebreak_and_hysteresis',
    'W17-P01.live_hazard_lifecycle',
    'W17-P02.live_smoke_radio_natural_paths',
    'W17-P03.live_terminal_priority_and_three_panels'
)

$regressionLockIds = @(
    'W17-H01.three_hazard_four_phase_lifecycle',
    'W17-E01.five_escape_ids_and_two_axes',
    'W17-N01.ending_catalog_19_and_samples',
    'W17-A01.selection_gate_not_runtime_referenced',
    'W17-A02.selection_gate_not_runtime_referenced',
    'W17-A03.selection_gate_not_runtime_referenced',
    'W17-P04.ko_en_qps_1280_layout',
    'W17-P05.keyboard_synthetic_gamepad_parity'
)
$matrixIds = @($baselineFailureIds + $regressionLockIds | Sort-Object -Unique)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $wave17Entry -PathType Leaf)) {
    throw "Wave 17 prerequisite entry point is missing: $wave17Entry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 18 baseline mismatch. Expected $BaselineCommit, observed $head"
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

function Compare-ExactSet([object[]]$Observed, [string[]]$Expected) {
    $observedValues = @($Observed | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $expectedValues = @($Expected | Sort-Object -Unique)
    return $observedValues.Count -eq $expectedValues.Count -and
        [string]::Join('|', $observedValues) -eq [string]::Join('|', $expectedValues)
}

function Test-Utf8NoBom([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    return $bytes.Length -lt 3 -or -not ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
}

function Find-Check([object[]]$Checks, [string]$Id) {
    return @($Checks | Where-Object { [string]$_.id -eq $Id } | Select-Object -First 1)[0]
}

$runStarted = [DateTime]::UtcNow
$shellEdition = if ([string]::IsNullOrWhiteSpace([string]$PSVersionTable.PSEdition)) { 'Desktop' } else { [string]$PSVersionTable.PSEdition }
$shellVersion = [string]$PSVersionTable.PSVersion
$shellExecutable = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName

# The prerequisite is intentionally allowed to return 1 on the RED baseline:
# its product failures are evidence, while its infrastructure result must PASS.
$wave17Arguments = @(
    '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $wave17Entry,
    '-RunId', $RunId, '-BaselineCommit', $BaselineCommit, '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$wave17Stage = Invoke-HiddenProcess 'fresh-wave17-full-gate' $shellExecutable $wave17Arguments `
    (Join-Path $workRoot 'wave18-wave17-stdout.log') (Join-Path $workRoot 'wave18-wave17-stderr.log')

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}

$editArguments = @(
    '-batchmode', '-nographics', '-quit', '-projectPath', $projectRoot,
    '-executeMethod', 'ParallelQA.Wave18GreenTransitionHardeningRunner.RunEditContracts',
    '-logFile', (Join-Path $workRoot 'unity-wave18-edit.log')
)
$editStage = Invoke-HiddenProcess 'wave18-edit-hardening' $UnityPath $editArguments `
    (Join-Path $workRoot 'wave18-edit-stdout.log') (Join-Path $workRoot 'wave18-edit-stderr.log')

$playArguments = @(
    '-batchmode', '-force-d3d11', '-projectPath', $projectRoot,
    '-executeMethod', 'ParallelQA.Wave18GreenTransitionHardeningRunner.RunPlayContracts',
    '-logFile', (Join-Path $workRoot 'unity-wave18-play.log')
)
$playStage = Invoke-HiddenProcess 'wave18-play-hardening' $UnityPath $playArguments `
    (Join-Path $workRoot 'wave18-play-stdout.log') (Join-Path $workRoot 'wave18-play-stderr.log')

$wave17Summary = Read-Json (Join-Path $evidenceRoot 'wave17-summary.json')
$wave16Summary = Read-Json (Join-Path $evidenceRoot 'wave16-summary.json')
$wave15Summary = Read-Json (Join-Path $evidenceRoot 'wave15-summary.json')
$wave14Gate = Read-Json (Join-Path $evidenceRoot 'wave14-qps-global-layout-gate.json')
$wave12Summary = Read-Json (Join-Path $evidenceRoot 'wave12-summary.json')
$smoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$addressables = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$wave17Edit = Read-Json (Join-Path $evidenceRoot 'wave17-edit-contracts.json')
$wave17Play = Read-Json (Join-Path $evidenceRoot 'wave17-play-contracts.json')
$wave18Edit = Read-Json (Join-Path $evidenceRoot 'wave18-edit-contracts.json')
$wave18Play = Read-Json (Join-Path $evidenceRoot 'wave18-play-contracts.json')
$privacy = Read-Json (Join-Path $evidenceRoot 'wave18-privacy-schema-evidence.json')
$art = Read-Json (Join-Path $evidenceRoot 'wave18-art-connection-evidence.json')
$playEvidence = Read-Json (Join-Path $evidenceRoot 'wave18-play-observation-evidence.json')

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($null -eq $wave17Summary -or $wave17Summary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 17 prerequisite summary is missing or infrastructure FAIL')
}
if ($null -eq $wave16Summary -or $wave16Summary.overall -ne 'GREEN' -or $wave16Summary.productOverall -ne 'PASS' -or $wave16Summary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 16 prerequisite did not remain GREEN')
}
if ($null -eq $wave15Summary -or $wave15Summary.overall -ne 'GREEN' -or $wave15Summary.productOverall -ne 'PASS' -or $wave15Summary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 15 prerequisite did not remain GREEN')
}
if ($null -eq $wave14Gate -or $wave14Gate.productOverall -ne 'PASS' -or $wave14Gate.infrastructureOverall -ne 'PASS' -or
    [int]$wave14Gate.targetCount -ne 10 -or [int]$wave14Gate.passedTargets -ne 10) {
    $infrastructureFailures.Add('qps-long global layout lock is not 10/10 PASS')
}
if ($null -eq $wave12Summary -or $wave12Summary.compile -notmatch '^PASS' -or
    $wave12Summary.windowsDevelopmentBuild -ne 'PASS' -or $wave12Summary.hiddenSmoke -ne 'PASS' -or
    $wave12Summary.addressables -notmatch '^PASS') {
    $infrastructureFailures.Add('compile/Windows build/hidden smoke/Addressables lock did not remain PASS')
}
if ($null -eq $smoke -or $smoke.result -ne 'PASS' -or [double]$smoke.observedSeconds -lt $MinimumSmokeSeconds -or
    -not [bool]$smoke.aliveAtMinimum -or -not [bool]$smoke.respondingAtMinimum) {
    $infrastructureFailures.Add("hidden Windows smoke did not remain alive/responding for at least $MinimumSmokeSeconds seconds")
}
if ($null -eq $addressables -or $addressables.overall -ne 'PASS') {
    $infrastructureFailures.Add('Addressables post-smoke contract did not remain PASS')
}
foreach ($report in @($wave17Edit, $wave17Play, $wave18Edit, $wave18Play)) {
    if ($null -eq $report -or $report.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add('a fresh Wave 17/18 machine-readable report is missing or infrastructure FAIL')
    }
}
if ([int]$editStage.exitCode -ne 0) { $infrastructureFailures.Add("Wave 18 Edit Unity stage exited $($editStage.exitCode)") }
if ([int]$playStage.exitCode -ne 0) { $infrastructureFailures.Add("Wave 18 Play Unity stage exited $($playStage.exitCode)") }
if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log') -PathType Leaf) {
    $infrastructureFailures.Add('raw Windows Player log escaped quarantine into durable evidence')
}

$sourceChecks = @()
foreach ($report in @($wave17Edit, $wave17Play)) { if ($null -ne $report) { $sourceChecks += @($report.checks) } }
$sourceFailureIds = @($sourceChecks | Where-Object { $_.status -eq 'FAIL' } | ForEach-Object { [string]$_.id })
$sourceExpectedGaps = @($sourceChecks | Where-Object { $_.status -eq 'EXPECTED_GAP' })
if ($BaselineCommit -eq $redBaseline -and -not (Compare-ExactSet $sourceFailureIds $baselineFailureIds)) {
    $infrastructureFailures.Add("exact fac8545 baseline did not reproduce all 15 Wave 17 failure IDs; observed=$([string]::Join(',', $sourceFailureIds))")
}
if ($sourceExpectedGaps.Count -ne 0) {
    $infrastructureFailures.Add('Wave 17 source failures must not be hidden as EXPECTED_GAP')
}
foreach ($id in $regressionLockIds) {
    $check = Find-Check $sourceChecks $id
    if ($null -eq $check -or [string]$check.status -ne 'PASS') {
        $infrastructureFailures.Add("fresh Wave 17 regression lock is not PASS: $id")
    }
}

$allChecks = @()
foreach ($report in @($wave18Edit, $wave18Play)) { if ($null -ne $report) { $allChecks += @($report.checks) } }
$productMatrixChecks = @($allChecks | Where-Object { $matrixIds -contains [string]$_.id })
$observedMatrixIds = @($productMatrixChecks | ForEach-Object { [string]$_.id })
if (-not (Compare-ExactSet $observedMatrixIds $matrixIds) -or $productMatrixChecks.Count -ne $matrixIds.Count) {
    $infrastructureFailures.Add("Wave 18 matrix must contain each of the 15 transition IDs and 8 locks exactly once; observed=$([string]::Join(',', $observedMatrixIds))")
}
if (@($allChecks | Where-Object { $_.status -eq 'EXPECTED_GAP' }).Count -ne 0) {
    $infrastructureFailures.Add('Wave 18 must report product failures as FAIL, never EXPECTED_GAP')
}

$productFailures = @($productMatrixChecks | Where-Object { $_.status -eq 'FAIL' })
$productPasses = @($productMatrixChecks | Where-Object { $_.status -eq 'PASS' })
$infrastructureChecks = @($allChecks | Where-Object { $_.status -eq 'INFRA_FAIL' })
if ($infrastructureChecks.Count -gt 0) {
    $infrastructureFailures.Add("Wave 18 reports contain $($infrastructureChecks.Count) infrastructure check failure(s)")
}

$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($productFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$overall = if ($infrastructureOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'PASS') { 'GREEN' } else { 'RED' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave18GreenTransitionGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"

$correctedChecks = @(
    'W17-O01.snapshot_and_private_log',
    'W17-P01.live_hazard_lifecycle',
    'W17-P02.live_smoke_radio_natural_paths',
    'W17-P03.live_terminal_priority_and_three_panels',
    'W17-A01.selection_gate_not_runtime_referenced',
    'W17-A02.selection_gate_not_runtime_referenced',
    'W17-A03.selection_gate_not_runtime_referenced'
) | ForEach-Object {
    $check = Find-Check $productMatrixChecks $_
    [ordered]@{ id = $_; status = if ($null -eq $check) { 'MISSING' } else { [string]$check.status }; actual = if ($null -eq $check) { 'missing' } else { [string]$check.actual } }
}

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 18 independent green-transition hardening gate'
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
    sourceWave17Baseline = [ordered]@{
        expectedFailureCount = 15
        expectedFailureIds = $baselineFailureIds
        observedFailureCount = $sourceFailureIds.Count
        observedFailureIds = $sourceFailureIds
        exactBaselineResult = if ($BaselineCommit -eq $redBaseline -and (Compare-ExactSet $sourceFailureIds $baselineFailureIds)) { 'PASS 15/15' } elseif ($BaselineCommit -eq $redBaseline) { 'FAIL' } else { 'NOT_APPLICABLE_POST_BASELINE' }
        expectedGapCount = $sourceExpectedGaps.Count
    }
    regressionLocks = [ordered]@{
        expectedCount = 8
        ids = $regressionLockIds
        sourcePassCount = @($regressionLockIds | Where-Object { $c = Find-Check $sourceChecks $_; $null -ne $c -and $c.status -eq 'PASS' }).Count
    }
    hardenedMatrix = [ordered]@{
        total = $matrixIds.Count
        passed = $productPasses.Count
        failed = $productFailures.Count
        failureIds = @($productFailures | ForEach-Object { [string]$_.id })
        passIds = @($productPasses | ForEach-Object { [string]$_.id })
        expectedGapCount = 0
    }
    correctedGateChecks = $correctedChecks
    greenLocks = [ordered]@{
        wave15 = if ($null -ne $wave15Summary) { [string]$wave15Summary.overall } else { 'MISSING' }
        wave16 = if ($null -ne $wave16Summary) { [string]$wave16Summary.overall } else { 'MISSING' }
        qpsLong = if ($null -ne $wave14Gate) { "$($wave14Gate.passedTargets)/$($wave14Gate.targetCount) $($wave14Gate.productOverall)" } else { 'MISSING' }
        compile = if ($null -ne $wave12Summary) { [string]$wave12Summary.compile } else { 'MISSING' }
        windowsDevelopmentBuild = if ($null -ne $wave12Summary) { [string]$wave12Summary.windowsDevelopmentBuild } else { 'MISSING' }
        hiddenSmoke = if ($null -ne $smoke) { "$($smoke.result) $($smoke.observedSeconds)s" } else { 'MISSING' }
        addressables = if ($null -ne $addressables) { [string]$addressables.overall } else { 'MISSING' }
        rawPlayerLog = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log')) { 'FAIL_PRESENT' } else { 'PASS_QUARANTINED' }
    }
    privacySchema = if ($null -ne $privacy) { [string]$privacy.result } else { 'MISSING' }
    artConnection = if ($null -ne $art) { "$($art.result) $($art.mode)" } else { 'MISSING' }
    liveObservation = if ($null -ne $playEvidence) { 'GENERATED' } else { 'MISSING' }
    infrastructureFailures = $infrastructureFailures.ToArray()
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    steamReadyClaim = $false
    greenTransition = 'Only zero product FAIL with all 23 matrix IDs present and infrastructure PASS is GREEN. Synthetic gamepad and Windows build never satisfy physical gamepad or Steam gates.'
    exactRerun = $exactRerun
    stages = @($wave17Stage, $editStage, $playStage)
    exitCode = $exitCode
}

$jsonPath = Join-Path $evidenceRoot 'wave18-summary.json'
$txtPath = Join-Path $evidenceRoot 'wave18-summary.txt'
[System.IO.File]::WriteAllText($jsonPath, ($summary | ConvertTo-Json -Depth 20) + [Environment]::NewLine, $utf8NoBom)
[System.IO.File]::WriteAllLines($txtPath, @(
    'Wave 18 independent green-transition hardening gate'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "PowerShell: $shellEdition $shellVersion"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Source Wave 17 exact baseline: $($summary.sourceWave17Baseline.exactBaselineResult); EXPECTED_GAP=$($summary.sourceWave17Baseline.expectedGapCount)"
    "Hardened matrix PASS/FAIL: $($productPasses.Count)/$($productFailures.Count) of $($matrixIds.Count)"
    "Hardened FAIL IDs: $([string]::Join(', ', @($summary.hardenedMatrix.failureIds)))"
    "Wave 15/Wave 16/qps: $($summary.greenLocks.wave15)/$($summary.greenLocks.wave16)/$($summary.greenLocks.qpsLong)"
    "Compile/Build/Smoke/Addressables: $($summary.greenLocks.compile)/$($summary.greenLocks.windowsDevelopmentBuild)/$($summary.greenLocks.hiddenSmoke)/$($summary.greenLocks.addressables)"
    "Privacy/Art/Raw player log: $($summary.privacySchema)/$($summary.artConnection)/$($summary.greenLocks.rawPlayerLog)"
    "Infrastructure failures: $($infrastructureFailures.Count)"
    'Physical gamepad: UNVERIFIED'
    'Steam: NOT_READY'
    "Exit code: $exitCode (0 GREEN, 2 product RED, 1 infrastructure FAIL)"
    "Rerun: $exactRerun"
), $utf8NoBom)

$compatibility = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    powershellEdition = $shellEdition
    powershellVersion = $shellVersion
    jsonUtf8NoBom = Test-Utf8NoBom $jsonPath
    textUtf8NoBom = Test-Utf8NoBom $txtPath
    rawPlayerLogQuarantined = -not (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log'))
}
$compatibility.overall = if ($compatibility.jsonUtf8NoBom -and $compatibility.textUtf8NoBom -and $compatibility.rawPlayerLogQuarantined) { 'PASS' } else { 'FAIL' }
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave18-powershell-compatibility.json'), ($compatibility | ConvertTo-Json -Depth 8) + [Environment]::NewLine, $utf8NoBom)
if ($compatibility.overall -ne 'PASS') {
    Write-Error 'Wave 18 PowerShell/UTF-8/raw-log compatibility failed.'
    exit 1
}

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "SOURCE_WAVE17_FAILURES=$($sourceFailureIds.Count)"
Write-Output "HARDENED_PASS=$($productPasses.Count)"
Write-Output "HARDENED_FAIL=$($productFailures.Count)"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output 'STEAM=NOT_READY'
Write-Output "EVIDENCE=$evidenceRoot"
exit $exitCode
