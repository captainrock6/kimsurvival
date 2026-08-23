[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = 'bd1c580bb53bdd662877efd1600c97500057c3ee',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(5, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$wave12Entry = Join-Path $PSScriptRoot 'Invoke-Wave12FiveDayUiGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $wave12Entry -PathType Leaf)) {
    throw "Wave 12 prerequisite entry point is missing: $wave12Entry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 14 baseline mismatch. Expected $BaselineCommit, observed $head"
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

$runStarted = [DateTime]::UtcNow
$shellEdition = if ([string]::IsNullOrWhiteSpace([string]$PSVersionTable.PSEdition)) { 'Desktop' } else { [string]$PSVersionTable.PSEdition }
$shellVersion = [string]$PSVersionTable.PSVersion
$shellExecutable = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName

$wave12Arguments = @(
    '-NoLogo',
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $wave12Entry,
    '-RunId', $RunId,
    '-BaselineCommit', $BaselineCommit,
    '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$wave12Stage = Invoke-HiddenProcess 'wave12-prerequisite-full-gate' $shellExecutable $wave12Arguments `
    (Join-Path $workRoot 'wave14-wave12-stdout.log') (Join-Path $workRoot 'wave14-wave12-stderr.log')

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}

$wave14Arguments = @(
    '-batchmode',
    '-nographics',
    '-quit',
    '-projectPath', $projectRoot,
    '-executeMethod', 'ParallelQA.Wave14QpsGlobalLayoutGateRunner.RunEvidenceContracts',
    '-logFile', (Join-Path $workRoot 'unity-wave14-qps-global-layout.log')
)
$wave14Stage = Invoke-HiddenProcess 'wave14-qps-global-layout-contracts' $UnityPath $wave14Arguments `
    (Join-Path $workRoot 'wave14-unity-stdout.log') (Join-Path $workRoot 'wave14-unity-stderr.log')

$gate = Read-Json (Join-Path $evidenceRoot 'wave14-qps-global-layout-gate.json')
$wave12Summary = Read-Json (Join-Path $evidenceRoot 'wave12-summary.json')
$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($wave12Stage.exitCode -ne 0) { $infrastructureFailures.Add("Wave 12 prerequisite exited $($wave12Stage.exitCode)") }
if ($wave14Stage.exitCode -ne 0) { $infrastructureFailures.Add("Wave 14 Unity evidence stage exited $($wave14Stage.exitCode)") }
if ($null -eq $wave12Summary -or $wave12Summary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('Wave 12 prerequisite did not report infrastructure PASS')
}
if ($null -eq $gate) {
    $infrastructureFailures.Add('Wave 14 machine-readable report is missing')
} elseif ($gate.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('Wave 14 runner reported infrastructure FAIL')
}

$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($null -eq $gate) { 'FAIL' } else { [string]$gate.productOverall }
$overall = if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'RED_EXPECTED_GAP') { 'RED' } else { 'GREEN' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }

$result = [ordered]@{
    schemaVersion = 1
    title = 'Wave 14 qps-long global 1280x800 layout gate command result'
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
    infrastructureFailures = $infrastructureFailures.ToArray()
    exactReportedBaselineReproduced = if ($null -eq $gate) { $false } else { [bool]$gate.exactSixOfTenBaselineReproduced }
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    exitCode = $exitCode
    exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave14QpsGlobalLayoutGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"
    stages = @($wave12Stage, $wave14Stage)
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave14-command-results.json'), ($result | ConvertTo-Json -Depth 12) + [Environment]::NewLine, $utf8NoBom)
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave14-command-results.txt'), @(
    'Wave 14 qps-long global layout gate command result'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "PowerShell: $shellEdition $shellVersion"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Exact reported 6/10 baseline reproduced: $($result.exactReportedBaselineReproduced)"
    "Physical gamepad: UNVERIFIED"
    "Steam: NOT_READY"
    "Exit code: $exitCode (0 GREEN, 2 RED_EXPECTED_GAP, 1 unexpected/infrastructure failure)"
    "Rerun: $($result.exactRerun)"
), $utf8NoBom)

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "EXACT_BASELINE_REPRODUCED=$($result.exactReportedBaselineReproduced)"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output 'STEAM=NOT_READY'
Write-Output "EVIDENCE=$evidenceRoot"
exit $exitCode
