[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '635725b3e2679a7d6d4f66c09b137575bac374c8',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(5, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$wave15Entry = Join-Path $PSScriptRoot 'Invoke-Wave15CampaignMapGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$redBaseline = '635725b3e2679a7d6d4f66c09b137575bac374c8'

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $wave15Entry -PathType Leaf)) {
    throw "Wave 15 prerequisite entry point is missing: $wave15Entry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 16 baseline mismatch. Expected $BaselineCommit, observed $head"
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

$runStarted = [DateTime]::UtcNow
$shellEdition = if ([string]::IsNullOrWhiteSpace([string]$PSVersionTable.PSEdition)) { 'Desktop' } else { [string]$PSVersionTable.PSEdition }
$shellVersion = [string]$PSVersionTable.PSVersion
$shellExecutable = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName

$wave15Arguments = @(
    '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $wave15Entry,
    '-RunId', $RunId, '-BaselineCommit', $BaselineCommit, '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$wave15Stage = Invoke-HiddenProcess 'wave15-full-green-prerequisite' $shellExecutable $wave15Arguments `
    (Join-Path $workRoot 'wave16-wave15-stdout.log') (Join-Path $workRoot 'wave16-wave15-stderr.log')

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}

$editArguments = @(
    '-batchmode', '-nographics', '-quit', '-projectPath', $projectRoot,
    '-executeMethod', 'ParallelQA.Wave16HazardEndingRedFirstRunner.RunEditContracts',
    '-logFile', (Join-Path $workRoot 'unity-wave16-edit.log')
)
$editStage = Invoke-HiddenProcess 'wave16-hazard-ending-edit' $UnityPath $editArguments `
    (Join-Path $workRoot 'wave16-edit-stdout.log') (Join-Path $workRoot 'wave16-edit-stderr.log')

$playArguments = @(
    '-batchmode', '-force-d3d11', '-projectPath', $projectRoot,
    '-executeMethod', 'ParallelQA.Wave16HazardEndingRedFirstRunner.RunPlayContracts',
    '-logFile', (Join-Path $workRoot 'unity-wave16-play.log')
)
$playStage = Invoke-HiddenProcess 'wave16-hazard-ending-play' $UnityPath $playArguments `
    (Join-Path $workRoot 'wave16-play-stdout.log') (Join-Path $workRoot 'wave16-play-stderr.log')

$wave15Summary = Read-Json (Join-Path $evidenceRoot 'wave15-summary.json')
$wave15Edit = Read-Json (Join-Path $evidenceRoot 'wave15-edit-contracts.json')
$wave15Play = Read-Json (Join-Path $evidenceRoot 'wave15-play-contracts.json')
$wave14Gate = Read-Json (Join-Path $evidenceRoot 'wave14-qps-global-layout-gate.json')
$wave12Summary = Read-Json (Join-Path $evidenceRoot 'wave12-summary.json')
$edit = Read-Json (Join-Path $evidenceRoot 'wave16-edit-contracts.json')
$play = Read-Json (Join-Path $evidenceRoot 'wave16-play-contracts.json')

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
# Unity runners deliberately return a non-zero code for product RED. The fresh
# machine-readable reports below are authoritative for separating product gaps
# from infrastructure failures; a crash or missing report is still INFRA_FAIL.
if ($null -eq $wave15Summary -or $wave15Summary.overall -ne 'GREEN' -or
    $wave15Summary.productOverall -ne 'PASS' -or $wave15Summary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 15 campaign/map prerequisite did not remain full GREEN')
}
foreach ($report in @($wave15Edit, $wave15Play)) {
    if ($null -eq $report -or $report.overall -ne 'GREEN' -or $report.productOverall -ne 'PASS' -or $report.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add('a fresh Wave 15 Edit/Play prerequisite report is missing or not GREEN')
    }
}
if ($null -eq $wave14Gate -or $wave14Gate.productOverall -ne 'PASS' -or $wave14Gate.infrastructureOverall -ne 'PASS' -or
    [int]$wave14Gate.targetCount -ne 10 -or [int]$wave14Gate.passedTargets -ne 10) {
    $infrastructureFailures.Add('qps-long global layout lock is not a fresh 10/10 PASS')
}
if ($null -eq $wave12Summary -or $wave12Summary.infrastructureOverall -ne 'PASS' -or
    $wave12Summary.compile -notmatch '^PASS' -or $wave12Summary.windowsDevelopmentBuild -ne 'PASS' -or
    $wave12Summary.hiddenSmoke -ne 'PASS' -or $wave12Summary.addressables -notmatch '^PASS') {
    $infrastructureFailures.Add('compile/build/hidden-smoke/Addressables regression lock did not remain PASS')
}
foreach ($report in @($edit, $play)) {
    if ($null -eq $report -or $report.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add('a Wave 16 machine-readable report is missing or infrastructure FAIL')
    }
}

$allChecks = @()
foreach ($report in @($edit, $play)) { if ($null -ne $report) { $allChecks += @($report.checks) } }
$expectedGaps = @($allChecks | Where-Object { $_.status -eq 'EXPECTED_GAP' -and $_.classification -eq 'PRODUCT_EXPECTED_GAP' })
$productFailures = @($allChecks | Where-Object { $_.status -eq 'FAIL' -and $_.classification -eq 'PRODUCT_REGRESSION' })
$infrastructureChecks = @($allChecks | Where-Object { $_.status -eq 'INFRA_FAIL' })
$unverified = @($allChecks | Where-Object { $_.status -eq 'UNVERIFIED' })
if ($infrastructureChecks.Count -gt 0) { $infrastructureFailures.Add("Wave 16 reports contain $($infrastructureChecks.Count) infrastructure check failure(s)") }
if ($BaselineCommit -eq $redBaseline -and $expectedGaps.Count -eq 0) {
    $infrastructureFailures.Add('the exact RED baseline unexpectedly produced zero product gaps; inspect baseline identity and public contract discovery')
}
if ($BaselineCommit -ne $redBaseline -and $expectedGaps.Count -gt 0) {
    $infrastructureFailures.Add('EXPECTED_GAP is only permitted on the exact 635725b RED baseline')
}

$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($productFailures.Count -gt 0) { 'FAIL' } elseif ($expectedGaps.Count -gt 0) { 'RED_EXPECTED_GAP' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'RED_EXPECTED_GAP') { 'RED' } else { 'GREEN' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave16HazardEndingGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 16 hazard, multi-escape, and behavioral-ending RED-first gate'
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
    wave15Prerequisite = if ($null -ne $wave15Summary) { [string]$wave15Summary.overall } else { 'MISSING' }
    currentGreenLocks = [ordered]@{
        wave15CampaignMap = if ($null -ne $wave15Summary) { [string]$wave15Summary.overall } else { 'MISSING' }
        qpsLong = if ($null -ne $wave14Gate) { "$($wave14Gate.passedTargets)/$($wave14Gate.targetCount) $($wave14Gate.productOverall)" } else { 'MISSING' }
        compile = if ($null -ne $wave12Summary) { [string]$wave12Summary.compile } else { 'MISSING' }
        windowsDevelopmentBuild = if ($null -ne $wave12Summary) { [string]$wave12Summary.windowsDevelopmentBuild } else { 'MISSING' }
        hiddenSmoke = if ($null -ne $wave12Summary) { [string]$wave12Summary.hiddenSmoke } else { 'MISSING' }
        addressables = if ($null -ne $wave12Summary) { [string]$wave12Summary.addressables } else { 'MISSING' }
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
        severity = [string]$_.severity
        actual = [string]$_.actual
        reproduction = [string]$_.reproduction
        recommendedFiles = [string]$_.recommendedFiles
    } })
    infrastructureFailures = $infrastructureFailures.ToArray()
    unverifiedIds = @($unverified | ForEach-Object { [string]$_.id })
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    steamReadyClaim = $false
    greenTransition = 'On a post-implementation baseline, the same command must keep fresh Wave 15 GREEN and report zero Wave 16 EXPECTED_GAP/FAIL checks with infrastructure PASS.'
    exactRerun = $exactRerun
    stages = @($wave15Stage, $editStage, $playStage)
    exitCode = $exitCode
}

$jsonPath = Join-Path $evidenceRoot 'wave16-summary.json'
$txtPath = Join-Path $evidenceRoot 'wave16-summary.txt'
[System.IO.File]::WriteAllText($jsonPath, ($summary | ConvertTo-Json -Depth 16) + [Environment]::NewLine, $utf8NoBom)
[System.IO.File]::WriteAllLines($txtPath, @(
    'Wave 16 hazard, multi-escape, and behavioral-ending RED-first gate'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "PowerShell: $shellEdition $shellVersion"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Wave 15 prerequisite: $($summary.wave15Prerequisite)"
    "Expected product gaps: $($expectedGaps.Count) [$([string]::Join(', ', @($summary.expectedGapIds)))]"
    "Unexpected product failures: $($productFailures.Count)"
    "Infrastructure failures: $($infrastructureFailures.Count)"
    "Compile/Build/Smoke/Addressables: $($summary.currentGreenLocks.compile)/$($summary.currentGreenLocks.windowsDevelopmentBuild)/$($summary.currentGreenLocks.hiddenSmoke)/$($summary.currentGreenLocks.addressables)"
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
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave16-powershell-compatibility.json'), ($utf8Result | ConvertTo-Json -Depth 6) + [Environment]::NewLine, $utf8NoBom)
if ($utf8Result.overall -ne 'PASS') {
    Write-Error 'Wave 16 evidence encoding compatibility failed.'
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
