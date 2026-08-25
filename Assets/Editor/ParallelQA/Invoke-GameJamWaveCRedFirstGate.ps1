[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = 'da7919ed7314b97865a7c8cebb738d420cfeb512',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(6, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'The GameJam Wave C gate requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$searchNodeEntry = Join-Path $PSScriptRoot 'Invoke-GameJamSearchNodeRedFirstGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $searchNodeEntry -PathType Leaf)) {
    throw "GameJam search-node prerequisite entry point is missing: $searchNodeEntry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}
if (Test-Path -LiteralPath $workRoot) {
    throw "Work directory already exists; choose a fresh RunId: $workRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "GameJam Wave C baseline mismatch. Expected $BaselineCommit, observed $head"
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
        RedirectStandardOutput = $StandardOutput
        RedirectStandardError = $StandardError
    }
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

function Write-Utf8NoBom([string]$Path, [string]$Value) {
    [IO.File]::WriteAllText($Path, $Value, $utf8NoBom)
}

function Test-Utf8NoBom([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    $bytes = [IO.File]::ReadAllBytes($Path)
    return $bytes.Length -lt 3 -or -not ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
}

function Test-ReportPassPrefix($Report, [string]$IdPrefix) {
    if ($null -eq $Report) { return $false }
    return @($Report.checks | Where-Object {
        [string]$_.id -like "$IdPrefix*" -and [string]$_.status -eq 'PASS'
    }).Count -gt 0
}

$runStarted = [DateTime]::UtcNow
$shellEdition = if ([string]::IsNullOrWhiteSpace([string]$PSVersionTable.PSEdition)) { 'Desktop' } else { [string]$PSVersionTable.PSEdition }
$shellVersion = [string]$PSVersionTable.PSVersion
$shellExecutable = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
$stages = New-Object System.Collections.Generic.List[object]

# P08 consumes the two reports emitted by this fresh prerequisite. Passing the
# same RunId and baseline writes them directly into this run's evidence folder;
# no report from an older run is copied or accepted.
$searchArguments = @(
    '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $searchNodeEntry,
    '-RunId', $RunId, '-BaselineCommit', $BaselineCommit, '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$searchStage = Invoke-HiddenProcess 'fresh-gsn-green-prerequisite' $shellExecutable $searchArguments `
    (Join-Path $workRoot 'wave-c-gsn-stdout.log') (Join-Path $workRoot 'wave-c-gsn-stderr.log')
$stages.Add($searchStage)

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}
$prerequisiteFileNames = @(
    'gamejam-search-node-edit-contracts.json',
    'gamejam-search-node-play-contracts.json'
)
$prerequisiteCopyRoot = Join-Path $workRoot 'fresh-gsn-prerequisites'
New-Item -ItemType Directory -Path $prerequisiteCopyRoot -Force | Out-Null
foreach ($fileName in $prerequisiteFileNames) {
    $generatedPath = Join-Path $evidenceRoot $fileName
    if (Test-Path -LiteralPath $generatedPath -PathType Leaf) {
        Copy-Item -LiteralPath $generatedPath -Destination (Join-Path $prerequisiteCopyRoot $fileName) -Force
    }
}

foreach ($definition in @(
    [ordered]@{ name = 'wave-c-edit-contracts'; method = 'ParallelQA.GameJamWaveCRedFirstGateRunner.RunEditContracts'; play = $false },
    [ordered]@{ name = 'wave-c-play-contracts'; method = 'ParallelQA.GameJamWaveCRedFirstGateRunner.RunPlayContracts'; play = $true }
)) {
    $logName = $definition.name.Replace('-', '_')
    if ([bool]$definition.play) {
        foreach ($fileName in $prerequisiteFileNames) {
            $freshCopyPath = Join-Path $prerequisiteCopyRoot $fileName
            if (Test-Path -LiteralPath $freshCopyPath -PathType Leaf) {
                Copy-Item -LiteralPath $freshCopyPath -Destination (Join-Path $evidenceRoot $fileName) -Force
            }
        }
    }
    $arguments = @('-batchmode')
    if ([bool]$definition.play) {
        $arguments += '-force-d3d11'
    } else {
        $arguments += @('-nographics', '-quit')
    }
    $arguments += @(
        '-projectPath', $projectRoot,
        '-executeMethod', [string]$definition.method,
        '-logFile', (Join-Path $workRoot ("unity-$logName.log"))
    )
    $stage = Invoke-HiddenProcess ([string]$definition.name) $UnityPath $arguments `
        (Join-Path $workRoot ("$logName-stdout.log")) (Join-Path $workRoot ("$logName-stderr.log"))
    $stages.Add($stage)
}

$commandResults = [ordered]@{
    schemaVersion = 1
    title = 'GameJam Wave C command results'
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    stages = @($stages | ForEach-Object { $_ })
}
$commandResultsPath = Join-Path $evidenceRoot 'gamejam-wave-c-command-results.json'
$commandResultsTextPath = Join-Path $evidenceRoot 'gamejam-wave-c-command-results.txt'
Write-Utf8NoBom $commandResultsPath (($commandResults | ConvertTo-Json -Depth 12) + [Environment]::NewLine)
$commandLines = @('GameJam Wave C command results', "Run ID: $RunId", "Baseline: $BaselineCommit")
foreach ($stage in $stages) {
    $commandLines += "$($stage.name) | exit=$($stage.exitCode) | $($stage.command)"
}
Write-Utf8NoBom $commandResultsTextPath (($commandLines -join [Environment]::NewLine) + [Environment]::NewLine)

$editReport = Read-Json (Join-Path $evidenceRoot 'gamejam-wave-c-edit-contracts.json')
$playReport = Read-Json (Join-Path $evidenceRoot 'gamejam-wave-c-play-contracts.json')
$editEvidence = Read-Json (Join-Path $evidenceRoot 'gamejam-wave-c-edit-observation-evidence.json')
$playEvidence = Read-Json (Join-Path $evidenceRoot 'gamejam-wave-c-play-observation-evidence.json')
$searchEditReport = Read-Json (Join-Path $evidenceRoot 'gamejam-search-node-edit-contracts.json')
$searchPlayReport = Read-Json (Join-Path $evidenceRoot 'gamejam-search-node-play-contracts.json')

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
foreach ($stage in $stages) {
    if ([int]$stage.exitCode -ne 0) {
        $infrastructureFailures.Add("$($stage.name) exited $($stage.exitCode)")
    }
}
foreach ($report in @($searchEditReport, $searchPlayReport)) {
    if ($null -eq $report -or [string]$report.infrastructureOverall -ne 'PASS' -or
        [string]$report.runId -ne $RunId -or [string]$report.baselineCommit -ne $BaselineCommit) {
        $infrastructureFailures.Add('a fresh same-run GSN prerequisite report is missing, identity-mismatched, or infrastructure FAIL')
    }
}
foreach ($fileName in $prerequisiteFileNames) {
    if (-not (Test-Path -LiteralPath (Join-Path $prerequisiteCopyRoot $fileName) -PathType Leaf)) {
        $infrastructureFailures.Add("fresh prerequisite copy is missing: $fileName")
    }
}
foreach ($required in @(
    [ordered]@{ report = $searchEditReport; id = 'GSN-E05' },
    [ordered]@{ report = $searchPlayReport; id = 'GSN-P05' },
    [ordered]@{ report = $searchPlayReport; id = 'GSN-P10' }
)) {
    if (-not (Test-ReportPassPrefix $required.report ([string]$required.id))) {
        $infrastructureFailures.Add("fresh same-run prerequisite $($required.id) is not PASS")
    }
}
foreach ($report in @($editReport, $playReport)) {
    if ($null -eq $report -or [string]$report.infrastructureOverall -ne 'PASS' -or
        [string]$report.runId -ne $RunId -or [string]$report.baselineCommit -ne $BaselineCommit) {
        $infrastructureFailures.Add('a fresh Wave C report is missing, identity-mismatched, or infrastructure FAIL')
    }
}
if ($null -eq $editEvidence -or $null -eq $playEvidence) {
    $infrastructureFailures.Add('structured Wave C Edit or Play observation evidence is missing')
}

$checks = @()
foreach ($report in @($editReport, $playReport)) {
    if ($null -ne $report) { $checks += @($report.checks) }
}
$passes = @($checks | Where-Object { [string]$_.status -eq 'PASS' -and [string]$_.id -notlike 'GWC-I*' })
$expectedGaps = @($checks | Where-Object { [string]$_.status -eq 'EXPECTED_GAP' })
$productFailures = @($checks | Where-Object { [string]$_.status -eq 'FAIL' })
$humanRequired = @($checks | Where-Object { [string]$_.status -eq 'HUMAN_REQUIRED' })
$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($productFailures.Count -gt 0) { 'FAIL' } elseif ($expectedGaps.Count -gt 0) { 'RED_EXPECTED_GAP' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'PASS') { 'GREEN' } else { 'RED' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$passIds = @($passes | ForEach-Object { [string]$_.id })
$gapIds = @($expectedGaps | ForEach-Object { [string]$_.id })
$failureIds = @($productFailures | ForEach-Object { [string]$_.id })
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-GameJamWaveCRedFirstGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"

$summary = [ordered]@{
    schemaVersion = 1
    title = 'GameJam Wave C RED-first independent QA gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    powershell = [ordered]@{ edition = $shellEdition; version = $shellVersion }
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    waveC = [ordered]@{
        passed = $passes.Count
        expectedGaps = $expectedGaps.Count
        failed = $productFailures.Count
        humanRequired = $humanRequired.Count
        passIds = $passIds
        expectedGapIds = $gapIds
        failureIds = $failureIds
    }
    prerequisites = [ordered]@{
        searchEditReport = if ($null -eq $searchEditReport) { 'MISSING' } elseif (
            [string]$searchEditReport.runId -eq $RunId -and
            [string]$searchEditReport.baselineCommit -eq $BaselineCommit -and
            (Test-ReportPassPrefix $searchEditReport 'GSN-E05')) { 'PASS same-run GSN-E05' } else { 'FAIL' }
        searchPlayReport = if ($null -eq $searchPlayReport) { 'MISSING' } elseif (
            [string]$searchPlayReport.runId -eq $RunId -and
            [string]$searchPlayReport.baselineCommit -eq $BaselineCommit -and
            (Test-ReportPassPrefix $searchPlayReport 'GSN-P05') -and
            (Test-ReportPassPrefix $searchPlayReport 'GSN-P10')) { 'PASS same-run GSN-P05/P10' } else { 'FAIL' }
        copiedFreshFiles = $prerequisiteFileNames
        requiredPassIds = @('GSN-E05', 'GSN-P05', 'GSN-P10')
    }
    greenTransitionConditions = @(
        'protected sailcloth, flint, and radio parts are eligible-only, survive loss pressures, and count only eligible completed misses for 3/5 pity',
        'raft, smoke, and radio are simultaneously completable and each has a distinct ordered natural production interaction trace',
        'fail, cancel, weather wait, and retry are atomic while ending and album record exactly once',
        'one run commits and re-enters upper and basement through save/restore without changing escape resources',
        'KO, EN, and qps-long render three live core comic panels plus a modifier with no required-action clipping',
        'a representative seed produces a 25-35 minute production-input profile with grant, warp, and skip counters at zero',
        'fresh same-run GSN-E05, GSN-P05, and GSN-P10 are PASS; GJC-12, GJC-20, and GJC-23 remain HUMAN_REQUIRED'
    )
    infrastructureFailures = @($infrastructureFailures | ForEach-Object { [string]$_ })
    exactRerun = $exactRerun
    stages = @($stages | ForEach-Object { $_ })
    exitCode = $exitCode
}

$summaryPath = Join-Path $evidenceRoot 'gamejam-wave-c-summary.json'
$summaryTextPath = Join-Path $evidenceRoot 'gamejam-wave-c-summary.txt'
Write-Utf8NoBom $summaryPath (($summary | ConvertTo-Json -Depth 20) + [Environment]::NewLine)
$summaryLines = @(
    'GameJam Wave C RED-first independent QA gate'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Wave C PASS/EXPECTED_GAP/FAIL/HUMAN_REQUIRED: $($passes.Count)/$($expectedGaps.Count)/$($productFailures.Count)/$($humanRequired.Count)"
    "Expected gap IDs: $([string]::Join(', ', $gapIds))"
    "Product failure IDs: $([string]::Join(', ', $failureIds))"
    'GJC-12/GJC-20/GJC-23: HUMAN_REQUIRED'
    "Rerun: $exactRerun"
    "Exit code: $exitCode (0 GREEN, 2 product RED, 1 infrastructure FAIL)"
)
Write-Utf8NoBom $summaryTextPath (($summaryLines -join [Environment]::NewLine) + [Environment]::NewLine)

$compatibility = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    shellEdition = $shellEdition
    shellVersion = $shellVersion
    windowsPowerShell51CompatibleSurface = $true
    commandResultsUtf8NoBom = Test-Utf8NoBom $commandResultsPath
    summaryUtf8NoBom = Test-Utf8NoBom $summaryPath
}
$compatibility.result = if ($compatibility.commandResultsUtf8NoBom -and $compatibility.summaryUtf8NoBom) { 'PASS' } else { 'FAIL' }
$compatibilityPath = Join-Path $evidenceRoot 'gamejam-wave-c-powershell-compatibility.json'
$compatibilityTextPath = Join-Path $evidenceRoot 'gamejam-wave-c-powershell-compatibility.txt'
Write-Utf8NoBom $compatibilityPath (($compatibility | ConvertTo-Json -Depth 6) + [Environment]::NewLine)
$compatibilityLines = @(
    'GameJam Wave C PowerShell compatibility'
    "Edition/Version: $shellEdition/$shellVersion"
    'Windows PowerShell 5.1 compatible surface: true'
    "Command/Summary UTF-8 no BOM: $($compatibility.commandResultsUtf8NoBom)/$($compatibility.summaryUtf8NoBom)"
    "Result: $($compatibility.result)"
)
Write-Utf8NoBom $compatibilityTextPath (($compatibilityLines -join [Environment]::NewLine) + [Environment]::NewLine)

if ($compatibility.result -ne 'PASS') { exit 1 }
Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "WAVE_C_PASS=$($passes.Count)"
Write-Output "WAVE_C_EXPECTED_GAP=$($expectedGaps.Count)"
Write-Output "WAVE_C_FAIL=$($productFailures.Count)"
Write-Output "GSN_PREREQUISITES=$($summary.prerequisites.searchEditReport)/$($summary.prerequisites.searchPlayReport)"
Write-Output "HUMAN_REQUIRED=$($humanRequired.Count)"
Write-Output "EVIDENCE=$evidenceRoot"
exit $exitCode
