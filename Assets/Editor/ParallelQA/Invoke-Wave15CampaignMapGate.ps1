[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '7796cf57568d0bad24595379e833e1dd9b4d8d3f',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(5, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$wave14Entry = Join-Path $PSScriptRoot 'Invoke-Wave14QpsGlobalLayoutGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$redBaseline = '7796cf57568d0bad24595379e833e1dd9b4d8d3f'

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $wave14Entry -PathType Leaf)) {
    throw "Wave 14 prerequisite entry point is missing: $wave14Entry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 15 baseline mismatch. Expected $BaselineCommit, observed $head"
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

$wave14Arguments = @(
    '-NoLogo',
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $wave14Entry,
    '-RunId', $RunId,
    '-BaselineCommit', $BaselineCommit,
    '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$wave14Stage = Invoke-HiddenProcess 'wave14-full-regression-prerequisite' $shellExecutable $wave14Arguments `
    (Join-Path $workRoot 'wave15-wave14-stdout.log') (Join-Path $workRoot 'wave15-wave14-stderr.log')

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}

$editArguments = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', $projectRoot,
    '-executeMethod', 'ParallelQA.Wave15CampaignMapRedFirstRunner.RunEditContracts',
    '-logFile', (Join-Path $workRoot 'unity-wave15-edit.log')
)
$editStage = Invoke-HiddenProcess 'wave15-campaign-map-edit' $UnityPath $editArguments `
    (Join-Path $workRoot 'wave15-edit-stdout.log') (Join-Path $workRoot 'wave15-edit-stderr.log')

$playArguments = @(
    '-batchmode', '-force-d3d11',
    '-projectPath', $projectRoot,
    '-executeMethod', 'ParallelQA.Wave15CampaignMapRedFirstRunner.RunPlayContracts',
    '-logFile', (Join-Path $workRoot 'unity-wave15-play.log')
)
$playStage = Invoke-HiddenProcess 'wave15-campaign-map-play' $UnityPath $playArguments `
    (Join-Path $workRoot 'wave15-play-stdout.log') (Join-Path $workRoot 'wave15-play-stderr.log')

$wave14Command = Read-Json (Join-Path $evidenceRoot 'wave14-command-results.json')
$wave14Gate = Read-Json (Join-Path $evidenceRoot 'wave14-qps-global-layout-gate.json')
$wave12Summary = Read-Json (Join-Path $evidenceRoot 'wave12-summary.json')
$edit = Read-Json (Join-Path $evidenceRoot 'wave15-edit-contracts.json')
$play = Read-Json (Join-Path $evidenceRoot 'wave15-play-contracts.json')

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($wave14Stage.exitCode -ne 0) { $infrastructureFailures.Add("Wave 14 prerequisite exited $($wave14Stage.exitCode)") }
if ($editStage.exitCode -ne 0) { $infrastructureFailures.Add("Wave 15 Edit stage exited $($editStage.exitCode)") }
if ($playStage.exitCode -ne 0) { $infrastructureFailures.Add("Wave 15 Play stage exited $($playStage.exitCode)") }
if ($null -eq $wave14Command -or $wave14Command.infrastructureOverall -ne 'PASS' -or $wave14Command.productOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 14 prerequisite did not remain GREEN/PASS')
}
if ($null -eq $wave14Gate -or $wave14Gate.infrastructureOverall -ne 'PASS' -or $wave14Gate.productOverall -ne 'PASS' -or
    [int]$wave14Gate.targetCount -ne 10 -or [int]$wave14Gate.passedTargets -ne 10) {
    $infrastructureFailures.Add('qps-long current lock is not a fresh 10/10 PASS')
}
if ($null -eq $wave12Summary -or $wave12Summary.infrastructureOverall -ne 'PASS' -or
    $wave12Summary.compile -notmatch '^PASS' -or $wave12Summary.windowsDevelopmentBuild -ne 'PASS' -or
    $wave12Summary.hiddenSmoke -ne 'PASS' -or $wave12Summary.addressables -notmatch '^PASS') {
    $infrastructureFailures.Add('compile/camp/bag/module/swim/build/smoke/Addressables lock did not remain PASS')
}
foreach ($report in @($edit, $play)) {
    if ($null -eq $report -or $report.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add('a Wave 15 machine-readable report is missing or infrastructure FAIL')
    }
}

$allChecks = @()
foreach ($report in @($edit, $play)) { if ($null -ne $report) { $allChecks += @($report.checks) } }
$expectedGaps = @($allChecks | Where-Object { $_.status -eq 'EXPECTED_GAP' -and $_.classification -eq 'PRODUCT_EXPECTED_GAP' })
$productFailures = @($allChecks | Where-Object { $_.status -eq 'FAIL' -and $_.classification -eq 'PRODUCT_REGRESSION' })
$infrastructureChecks = @($allChecks | Where-Object { $_.status -eq 'INFRA_FAIL' })
if ($infrastructureChecks.Count -gt 0) { $infrastructureFailures.Add("Wave 15 reports contain $($infrastructureChecks.Count) infrastructure check failure(s)") }
if ($BaselineCommit -eq $redBaseline -and $expectedGaps.Count -eq 0) {
    $infrastructureFailures.Add('the exact RED baseline unexpectedly produced zero product gaps; inspect baseline identity and product probe discovery')
}
if ($BaselineCommit -ne $redBaseline -and $expectedGaps.Count -gt 0) {
    $infrastructureFailures.Add('EXPECTED_GAP is only permitted on the exact 7796cf5 RED baseline')
}

$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($productFailures.Count -gt 0) { 'FAIL' } elseif ($expectedGaps.Count -gt 0) { 'RED_EXPECTED_GAP' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'RED_EXPECTED_GAP') { 'RED' } else { 'GREEN' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }

$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave15CampaignMapGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"
$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 15 50-day campaign and expedition-map RED-first gate'
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
    currentGreenLocks = [ordered]@{
        qpsLong = if ($null -ne $wave14Gate) { "$($wave14Gate.passedTargets)/$($wave14Gate.targetCount) PASS" } else { 'MISSING' }
        koEnPlacement = if ($null -ne $wave14Gate -and $wave14Gate.productOverall -eq 'PASS') { 'PASS via fresh Wave14/Wave3' } else { 'FAIL' }
        campPromptPlacementModuleBagSwim = if ($null -ne $wave12Summary -and $wave12Summary.infrastructureOverall -eq 'PASS') { 'PASS (Wave 12 Day-5 assertion superseded by canonical Day 50)' } else { 'FAIL' }
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
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    steamReadyClaim = $false
    greenTransition = 'On a post-implementation baseline, the same command must report infrastructure PASS, fresh Wave14 10/10 PASS, and zero Wave15 EXPECTED_GAP/FAIL checks.'
    exactRerun = $exactRerun
    stages = @($wave14Stage, $editStage, $playStage)
    exitCode = $exitCode
}

$jsonPath = Join-Path $evidenceRoot 'wave15-summary.json'
$txtPath = Join-Path $evidenceRoot 'wave15-summary.txt'
[System.IO.File]::WriteAllText($jsonPath, ($summary | ConvertTo-Json -Depth 14) + [Environment]::NewLine, $utf8NoBom)
[System.IO.File]::WriteAllLines($txtPath, @(
    'Wave 15 50-day campaign and expedition-map RED-first gate'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "PowerShell: $shellEdition $shellVersion"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Expected product gaps: $($expectedGaps.Count) [$([string]::Join(', ', @($summary.expectedGapIds)))]"
    "Unexpected product failures: $($productFailures.Count)"
    "Infrastructure failures: $($infrastructureFailures.Count)"
    "qps-long lock: $($summary.currentGreenLocks.qpsLong)"
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
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave15-powershell-compatibility.json'), ($utf8Result | ConvertTo-Json -Depth 6) + [Environment]::NewLine, $utf8NoBom)
if ($utf8Result.overall -ne 'PASS') {
    Write-Error 'Wave 15 evidence encoding compatibility failed.'
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
