[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = 'e4bbc03531d54e023f7a90f7a608871a47d26d55',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'The GameJam long-stay gate requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}
if (Test-Path -LiteralPath $workRoot) {
    throw "Work directory already exists; choose a fresh RunId: $workRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "GameJam long-stay baseline mismatch. Expected $BaselineCommit, observed $head"
}

New-Item -ItemType Directory -Path $workRoot -Force | Out-Null
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
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
        PassThru = $true
        RedirectStandardOutput = $StandardOutput
        RedirectStandardError = $StandardError
    }
    $process = Start-Process @parameters
    # Wait for the launched Unity process itself. Start-Process -Wait also waits
    # for descendants such as licensing/UPM helpers and can hang after Unity has
    # already returned an infrastructure error.
    $process.WaitForExit()
    $process.Refresh()
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

$runStarted = [DateTime]::UtcNow
$shellEdition = if ([string]::IsNullOrWhiteSpace([string]$PSVersionTable.PSEdition)) { 'Desktop' } else { [string]$PSVersionTable.PSEdition }
$shellVersion = [string]$PSVersionTable.PSVersion
$stages = New-Object System.Collections.Generic.List[object]

foreach ($definition in @(
    [ordered]@{ name = 'long-stay-edit-contracts'; method = 'ParallelQA.GameJamLongStayRedFirstGateRunner.RunEditContracts'; play = $false },
    [ordered]@{ name = 'long-stay-play-contracts'; method = 'ParallelQA.GameJamLongStayRedFirstGateRunner.RunPlayContracts'; play = $true }
)) {
    $logName = $definition.name.Replace('-', '_')
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
    title = 'GameJam long-stay ending command results'
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    stages = @($stages | ForEach-Object { $_ })
}
$commandResultsPath = Join-Path $evidenceRoot 'gamejam-long-stay-command-results.json'
$commandResultsTextPath = Join-Path $evidenceRoot 'gamejam-long-stay-command-results.txt'
Write-Utf8NoBom $commandResultsPath (($commandResults | ConvertTo-Json -Depth 12) + [Environment]::NewLine)
$commandLines = @('GameJam long-stay ending command results', "Run ID: $RunId", "Baseline: $BaselineCommit")
foreach ($stage in $stages) {
    $commandLines += "$($stage.name) | exit=$($stage.exitCode) | $($stage.command)"
}
Write-Utf8NoBom $commandResultsTextPath (($commandLines -join [Environment]::NewLine) + [Environment]::NewLine)

$editReport = Read-Json (Join-Path $evidenceRoot 'gamejam-long-stay-edit-contracts.json')
$playReport = Read-Json (Join-Path $evidenceRoot 'gamejam-long-stay-play-contracts.json')
$editEvidence = Read-Json (Join-Path $evidenceRoot 'gamejam-long-stay-edit-observation-evidence.json')
$playEvidence = Read-Json (Join-Path $evidenceRoot 'gamejam-long-stay-play-observation-evidence.json')

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
foreach ($stage in $stages) {
    if ([int]$stage.exitCode -ne 0) {
        $infrastructureFailures.Add("$($stage.name) exited $($stage.exitCode)")
    }
}
foreach ($report in @($editReport, $playReport)) {
    if ($null -eq $report -or [string]$report.infrastructureOverall -ne 'PASS' -or
        [string]$report.runId -ne $RunId -or [string]$report.baselineCommit -ne $BaselineCommit) {
        $infrastructureFailures.Add('a fresh long-stay report is missing, identity-mismatched, or infrastructure FAIL')
    }
}
if ($null -eq $editEvidence -or $null -eq $playEvidence) {
    $infrastructureFailures.Add('structured long-stay Edit or Play observation evidence is missing')
}

$checks = @()
foreach ($report in @($editReport, $playReport)) {
    if ($null -ne $report) { $checks += @($report.checks) }
}
$passes = @($checks | Where-Object { [string]$_.status -eq 'PASS' -and [string]$_.id -notlike 'GJLS-I*' })
$expectedGaps = @($checks | Where-Object { [string]$_.status -eq 'EXPECTED_GAP' })
$productFailures = @($checks | Where-Object { [string]$_.status -eq 'FAIL' })
$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$passIds = @($passes | ForEach-Object { [string]$_.id })
$gapIds = @($expectedGaps | ForEach-Object { [string]$_.id })
$failureIds = @($productFailures | ForEach-Object { [string]$_.id })
$productOverall = if ($infrastructureOverall -eq 'FAIL' -and $checks.Count -eq 0) { 'UNVERIFIED' } elseif ($productFailures.Count -gt 0) { 'FAIL' } elseif ($expectedGaps.Count -gt 0) { 'RED_EXPECTED_GAP' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'PASS') { 'GREEN' } else { 'RED' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-GameJamLongStayRedFirstGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit'"

$summary = [ordered]@{
    schemaVersion = 1
    title = 'GameJam long-stay endings RED-first independent QA gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    powershell = [ordered]@{ edition = $shellEdition; version = $shellVersion }
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    passed = $passes.Count
    expectedGaps = $expectedGaps.Count
    failed = $productFailures.Count
    passIds = $passIds
    expectedGapIds = $gapIds
    failureIds = $failureIds
    infrastructureFailures = @($infrastructureFailures | ForEach-Object { $_ })
    evidencePolicy = 'Play PASS requires production-live structured observation. Fixture/static bool/string, grant, warp, and skip cannot satisfy product checks.'
    greenCompletionCondition = 'Catalog 21/two stable IDs, both no-escape Day20 endings, early escape precedence, deterministic replay, terminal+album exactly once, 2x KO/EN/qps live comics with clipping 0, standard Day50 unchanged, and grant/warp/skip 0 all PASS.'
    exactRerun = $exactRerun
}
$summaryPath = Join-Path $evidenceRoot 'gamejam-long-stay-summary.json'
$summaryTextPath = Join-Path $evidenceRoot 'gamejam-long-stay-summary.txt'
Write-Utf8NoBom $summaryPath (($summary | ConvertTo-Json -Depth 12) + [Environment]::NewLine)
$summaryLines = @(
    'GameJam long-stay endings RED-first independent QA gate',
    "Run ID: $RunId",
    "Baseline: $BaselineCommit",
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall",
    "PASS/EXPECTED_GAP/FAIL: $($passes.Count)/$($expectedGaps.Count)/$($productFailures.Count)",
    "PASS IDs: $($passIds -join ',')",
    "EXPECTED_GAP IDs: $($gapIds -join ',')",
    "FAIL IDs: $($failureIds -join ',')",
    "Infrastructure failures: $(@($infrastructureFailures) -join ' | ')",
    "Exact rerun: $exactRerun"
)
Write-Utf8NoBom $summaryTextPath (($summaryLines -join [Environment]::NewLine) + [Environment]::NewLine)

$expectedFiles = @(
    'gamejam-long-stay-edit-contracts.json',
    'gamejam-long-stay-edit-contracts.txt',
    'gamejam-long-stay-edit-observation-evidence.json',
    'gamejam-long-stay-play-contracts.json',
    'gamejam-long-stay-play-contracts.txt',
    'gamejam-long-stay-play-observation-evidence.json',
    'gamejam-long-stay-command-results.json',
    'gamejam-long-stay-command-results.txt',
    'gamejam-long-stay-summary.json',
    'gamejam-long-stay-summary.txt'
)
$bomFailures = @($expectedFiles | Where-Object { -not (Test-Utf8NoBom (Join-Path $evidenceRoot $_)) })
$compatibility = [ordered]@{
    schemaVersion = 1
    title = 'GameJam long-stay PowerShell compatibility'
    runId = $RunId
    edition = $shellEdition
    version = $shellVersion
    status = if ($bomFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
    utf8NoBomFailures = $bomFailures
    usesWindowsPowerShell51CompatibleSyntax = $true
}
$compatibilityPath = Join-Path $evidenceRoot 'gamejam-long-stay-powershell-compatibility.json'
$compatibilityTextPath = Join-Path $evidenceRoot 'gamejam-long-stay-powershell-compatibility.txt'
Write-Utf8NoBom $compatibilityPath (($compatibility | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
Write-Utf8NoBom $compatibilityTextPath ((@(
    'GameJam long-stay PowerShell compatibility',
    "PowerShell: $shellEdition $shellVersion",
    "UTF-8 no BOM: $($compatibility.status)",
    "Failures: $($bomFailures -join ',')"
) -join [Environment]::NewLine) + [Environment]::NewLine)

Write-Output "GameJam long-stay gate: $overall"
Write-Output "Summary: $summaryPath"
exit $exitCode
