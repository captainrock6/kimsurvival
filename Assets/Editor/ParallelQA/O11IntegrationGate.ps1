[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$')]
    [string]$RunId,

    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$BaselineCommit = 'aa67a12bb38180f7cf2635a2a2bca3c403b5248a',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [switch]$IncludeBuild,

    [switch]$ReadinessOnly,

    [ValidateRange(6, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'The O11 gate requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$redBaseline = 'aa67a12bb38180f7cf2635a2a2bca3c403b5248a'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$effectiveRunId = if ($RunId.StartsWith('O11', [StringComparison]::Ordinal)) { $RunId } else { 'O11_' + $RunId }
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $effectiveRunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $effectiveRunId)
$prerequisiteEntry = Join-Path $PSScriptRoot 'Invoke-GameJamSearchNodeRedFirstGate.ps1'
$utf8NoBom = New-Object Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw "Unity Editor not found: $UnityPath" }
if (Test-Path -LiteralPath $evidenceRoot) { throw "Evidence already exists; use a fresh RunId: $evidenceRoot" }
if (Test-Path -LiteralPath $workRoot) { throw "Work output already exists; use a fresh RunId: $workRoot" }

$head = (& git -C $projectRoot rev-parse HEAD).Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit.ToLowerInvariant()) {
    throw "O11 baseline mismatch. Expected $BaselineCommit, observed $head"
}
if ($IncludeBuild -and $ReadinessOnly) {
    throw '-IncludeBuild and -ReadinessOnly are mutually exclusive.'
}
if (-not $IncludeBuild -and -not $ReadinessOnly -and $head -ne $redBaseline) {
    throw 'Post-integration O11 validation must include -IncludeBuild. Use -ReadinessOnly only for the non-GREEN compile/edit/play bridge preflight.'
}

New-Item -ItemType Directory -Path $workRoot | Out-Null
$env:KIM_PARALLEL_QA_RUN_ID = $effectiveRunId
$env:KIM_PARALLEL_QA_BASELINE = $BaselineCommit.ToLowerInvariant()
$startedUtc = [DateTime]::UtcNow
$stages = New-Object Collections.Generic.List[object]

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
    return [pscustomobject][ordered]@{
        name = $Name
        startedUtc = $started.ToString('O')
        completedUtc = [DateTime]::UtcNow.ToString('O')
        exitCode = $process.ExitCode
        command = (Quote-Argument $Executable) + ' ' + $argumentLine
        standardOutput = $StandardOutput
        standardError = $StandardError
    }
}

function Invoke-UnityStage([string]$Name, [string]$Method, [bool]$Play) {
    $arguments = @('-batchmode')
    if ($Play) { $arguments += '-force-d3d11' } else { $arguments += @('-nographics', '-quit') }
    $logPath = Join-Path $workRoot ("unity-" + $Name + '.log')
    $arguments += @('-projectPath', $projectRoot, '-executeMethod', $Method, '-logFile', $logPath)
    return Invoke-HiddenProcess $Name $UnityPath $arguments `
        (Join-Path $workRoot ($Name + '-stdout.log')) (Join-Path $workRoot ($Name + '-stderr.log'))
}

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}

if ($IncludeBuild) {
    if (-not (Test-Path -LiteralPath $prerequisiteEntry -PathType Leaf)) {
        throw "Build/smoke prerequisite entry is missing: $prerequisiteEntry"
    }
    $shellExecutable = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    $prerequisiteArgs = @(
        '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $prerequisiteEntry,
        '-RunId', $effectiveRunId, '-BaselineCommit', $BaselineCommit, '-UnityPath', $UnityPath,
        '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
    )
    $stages.Add((Invoke-HiddenProcess 'build-smoke-prerequisite' $shellExecutable $prerequisiteArgs `
        (Join-Path $workRoot 'build-smoke-stdout.log') (Join-Path $workRoot 'build-smoke-stderr.log')))
} else {
    New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
}

$stages.Add((Invoke-UnityStage 'compile' 'ParallelQA.ParallelQaRunner.RecordCompilePass' $false))
$stages.Add((Invoke-UnityStage 'edit' 'ParallelQA.O11IntegrationGateRunner.RunEditContracts' $false))
$stages.Add((Invoke-UnityStage 'play-render' 'ParallelQA.O11IntegrationGateRunner.RunPlayContracts' $true))

$product = Read-Json (Join-Path $evidenceRoot 'O11-product-report.json')
$edit = Read-Json (Join-Path $evidenceRoot 'O11-edit-evidence.json')
$compilePath = Join-Path $evidenceRoot 'compile-result.txt'
$compileText = if (Test-Path -LiteralPath $compilePath) { Get-Content -LiteralPath $compilePath -Raw -Encoding UTF8 } else { '' }
$compileErrorCount = if ($compileText -match 'Compiler errors:\s*(\d+)') { [int]$Matches[1] } else { -1 }
$compileWarningCount = if ($compileText -match 'Compiler warnings:\s*(\d+)') { [int]$Matches[1] } else { -1 }
$o11CompilerWarnings = @($compileText -split "`r?`n" | Where-Object { $_ -match 'warning CS\d+:' -and $_ -match 'Assets[\\/]Editor[\\/]ParallelQA[\\/]O11' })
$infrastructureFailures = New-Object Collections.Generic.List[string]

foreach ($stage in $stages) {
    if ($stage.name -eq 'build-smoke-prerequisite') {
        if ([int]$stage.exitCode -ne 0) { $infrastructureFailures.Add("$($stage.name) exited $($stage.exitCode)") }
    } elseif ([int]$stage.exitCode -ne 0) {
        $infrastructureFailures.Add("$($stage.name) exited $($stage.exitCode)")
    }
}
if ($compileErrorCount -ne 0 -or $o11CompilerWarnings.Count -ne 0) {
    $infrastructureFailures.Add("Unity compile has $compileErrorCount errors or $($o11CompilerWarnings.Count) O11-owned warnings")
}
if ($null -eq $edit -or [string]$edit.runId -ne $effectiveRunId -or [string]$edit.baselineCommit -ne $BaselineCommit) {
    $infrastructureFailures.Add('fresh O11 Edit evidence is missing or identity-mismatched')
}
if ($null -eq $product -or [string]$product.runId -ne $effectiveRunId -or [string]$product.baselineCommit -ne $BaselineCommit) {
    $infrastructureFailures.Add('fresh O11 Play/product report is missing or identity-mismatched')
}

$buildStatus = if ($ReadinessOnly) { 'NOT_RUN_READINESS_PREFLIGHT' } else { 'NOT_RUN_RED_BASELINE_POLICY' }
$lockSummary = $null
if ($IncludeBuild) {
    $lockSummary = Read-Json (Join-Path $evidenceRoot 'gamejam-search-node-summary.json')
    if ($null -eq $lockSummary -or [string]$lockSummary.overall -ne 'GREEN' -or
        [string]$lockSummary.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add('compile/build/smoke/Addressables/firewall prerequisite is not GREEN/PASS')
        $buildStatus = 'FAIL'
    } else {
        $buildStatus = 'PASS'
    }
}

$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($null -eq $product) { 'UNKNOWN' } else { [string]$product.productOverall }
$overall = if ($infrastructureOverall -eq 'FAIL') {
    'FAIL'
} elseif ($ReadinessOnly -and $productOverall -eq 'PASS') {
    'READY_FOR_FULL_GATE'
} elseif ($productOverall -eq 'PASS') {
    'GREEN'
} else {
    'RED'
}
$exitCode = if ($overall -eq 'GREEN' -or $overall -eq 'READY_FOR_FULL_GATE') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$exactRerun = "& '.\Assets\Editor\ParallelQA\O11IntegrationGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit'" +
    $(if ($IncludeBuild) { ' -IncludeBuild' } elseif ($ReadinessOnly) { ' -ReadinessOnly' } else { '' })
$exactFullRerun = "& '.\Assets\Editor\ParallelQA\O11IntegrationGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '<BRIDGE_INTEGRATED_FULL_SHA>' -IncludeBuild"

$summary = [ordered]@{
    schemaVersion = 1
    title = 'O11 independent edit/play/render/build integration gate'
    runId = $effectiveRunId
    baselineCommit = $BaselineCommit.ToLowerInvariant()
    startedUtc = $startedUtc.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    powershell = [ordered]@{ edition = [string]$PSVersionTable.PSEdition; version = [string]$PSVersionTable.PSVersion }
    mode = if ($ReadinessOnly) { 'READINESS_ONLY' } elseif ($IncludeBuild) { 'FULL' } else { 'RED_BASELINE' }
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    product = if ($null -eq $product) { $null } else { [ordered]@{
        passed = [int]$product.passed
        expectedGaps = [int]$product.expectedGaps
        failed = [int]$product.failed
        checks = @($product.checks)
    } }
    compile = if ($compileErrorCount -eq 0 -and $o11CompilerWarnings.Count -eq 0) {
        "PASS 0 errors / $compileWarningCount baseline warnings / 0 O11 warnings"
    } else { 'FAIL' }
    buildSmokeLocks = $buildStatus
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    infrastructureFailures = $infrastructureFailures.ToArray()
    stages = $stages.ToArray()
    exactRerun = $exactRerun
    exactFullRerunAfterBridge = $exactFullRerun
    exitCode = $exitCode
}

$summaryPath = Join-Path $evidenceRoot 'O11-summary.json'
[IO.File]::WriteAllText($summaryPath, ($summary | ConvertTo-Json -Depth 30) + [Environment]::NewLine, $utf8NoBom)
$text = @(
    'O11 independent integration gate'
    "Run ID: $effectiveRunId"
    "Baseline: $BaselineCommit"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Compile: $($summary.compile)"
    "Build/smoke locks: $buildStatus"
    "Product PASS/EXPECTED_GAP/FAIL: $($summary.product.passed)/$($summary.product.expectedGaps)/$($summary.product.failed)"
    'Physical gamepad: UNVERIFIED'
    'Steam: NOT_READY'
    "Rerun: $exactRerun"
    "Full rerun after bridge: $exactFullRerun"
    "Exit code: $exitCode (0 GREEN/readiness-pass, 2 product RED, 1 infrastructure failure)"
) -join [Environment]::NewLine
[IO.File]::WriteAllText((Join-Path $evidenceRoot 'O11-summary.txt'), $text + [Environment]::NewLine, $utf8NoBom)

Write-Output "O11=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "BUILD_SMOKE_LOCKS=$buildStatus"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output 'STEAM=NOT_READY'
Write-Output "EVIDENCE=$evidenceRoot"
exit $exitCode
