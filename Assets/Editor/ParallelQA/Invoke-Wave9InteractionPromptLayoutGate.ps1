[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = 'f95b192d45f04e36f173ae274e29a3684cce7bf0',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(5, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh run ID: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Prompt gate baseline mismatch. Expected $BaselineCommit, observed $head"
}

New-Item -ItemType Directory -Path $workRoot -Force | Out-Null
$env:KIM_PARALLEL_QA_RUN_ID = $RunId
$env:KIM_PARALLEL_QA_BASELINE = $BaselineCommit
$started = [DateTime]::UtcNow

function Quote-Argument([string]$Value) {
    if ($Value -match '[\s"]') { return '"' + $Value.Replace('"', '\"') + '"' }
    return $Value
}

function Invoke-UnityStage(
    [string]$Name,
    [string]$ExecuteMethod,
    [string]$LogFile,
    [bool]$Graphics,
    [bool]$SelfExiting
) {
    $arguments = New-Object System.Collections.Generic.List[string]
    $arguments.Add('-batchmode')
    if ($Graphics) { $arguments.Add('-force-d3d11') } else { $arguments.Add('-nographics') }
    if (-not $SelfExiting) { $arguments.Add('-quit') }
    $arguments.Add('-projectPath')
    $arguments.Add($projectRoot)
    $arguments.Add('-executeMethod')
    $arguments.Add($ExecuteMethod)
    $arguments.Add('-logFile')
    $arguments.Add($LogFile)
    $argumentLine = [string]::Join(' ', @($arguments | ForEach-Object { Quote-Argument $_ }))
    $stageStarted = [DateTime]::UtcNow
    $process = Start-Process -FilePath $UnityPath -ArgumentList $argumentLine -WindowStyle Hidden -Wait -PassThru
    return [ordered]@{
        name = $Name
        executeMethod = $ExecuteMethod
        startedUtc = $stageStarted.ToString('O')
        completedUtc = [DateTime]::UtcNow.ToString('O')
        exitCode = $process.ExitCode
        log = $LogFile
        command = (Quote-Argument $UnityPath) + ' ' + $argumentLine
    }
}

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

$baseScript = Join-Path $PSScriptRoot 'Invoke-Wave9SpatialCampContractGate.ps1'
$baseStdout = Join-Path $workRoot 'wave9-base-gate.stdout.log'
$baseStderr = Join-Path $workRoot 'wave9-base-gate.stderr.log'
$powershell = (Get-Process -Id $PID).Path
$baseArgs = @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $baseScript,
    '-RunId', $RunId,
    '-BaselineCommit', $BaselineCommit,
    '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$baseArgumentLine = [string]::Join(' ', @($baseArgs | ForEach-Object { Quote-Argument ([string]$_) }))
$baseStarted = [DateTime]::UtcNow
$baseProcess = Start-Process -FilePath $powershell -ArgumentList $baseArgumentLine -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $baseStdout -RedirectStandardError $baseStderr
$baseStage = [ordered]@{
    name = 'wave9-full-regression-gate'
    executeMethod = 'Invoke-Wave9SpatialCampContractGate.ps1'
    startedUtc = $baseStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    exitCode = $baseProcess.ExitCode
    stdout = $baseStdout
    stderr = $baseStderr
    command = "powershell -File Invoke-Wave9SpatialCampContractGate.ps1 -RunId '$RunId' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"
}

if (-not (Test-Path -LiteralPath $evidenceRoot -PathType Container)) {
    throw "The base Wave 9 gate did not create its fresh evidence directory. See $baseStdout and $baseStderr"
}

$stages = New-Object System.Collections.Generic.List[object]
$stages.Add($baseStage)
$stages.Add((Invoke-UnityStage 'prompt-layout-edit-contracts' 'ParallelQA.Wave9InteractionPromptLayoutGateRunner.RunEditContracts' (Join-Path $workRoot 'unity-prompt-layout-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'prompt-layout-play-contracts' 'ParallelQA.Wave9InteractionPromptLayoutGateRunner.RunPlayContracts' (Join-Path $workRoot 'unity-prompt-layout-play.log') $true $true))

$baseSummary = Read-Json (Join-Path $evidenceRoot 'wave9-summary.json')
$edit = Read-Json (Join-Path $evidenceRoot 'prompt-layout-edit-contracts.json')
$play = Read-Json (Join-Path $evidenceRoot 'prompt-layout-play-contracts.json')
$layout = Read-Json (Join-Path $evidenceRoot 'prompt-layout-evidence.json')
$build = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$smoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($baseProcess.ExitCode -notin @(0, 2) -or $null -eq $baseSummary -or $baseSummary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('Fresh Wave 9 compile/regression/build/smoke gate did not complete with infrastructure PASS')
}
foreach ($stage in @($stages | Where-Object { $_.name -like 'prompt-layout-*' })) {
    if ($stage.exitCode -ne 0) { $infrastructureFailures.Add("$($stage.name) Unity process exited $($stage.exitCode)") }
}
if ($null -eq $edit -or $edit.infrastructureOverall -ne 'PASS') { $infrastructureFailures.Add('Prompt Edit contract infrastructure did not pass') }
if ($null -eq $play -or $play.infrastructureOverall -ne 'PASS') { $infrastructureFailures.Add('Prompt Play contract infrastructure did not pass') }
if ($null -eq $layout -or @($layout.layouts).Count -ne 3 -or @($layout.targets).Count -ne 4) { $infrastructureFailures.Add('Prompt metric matrix is incomplete') }

$unexpected = @()
$expected = @()
foreach ($report in @($edit, $play)) {
    if ($null -eq $report) { continue }
    $unexpected += @($report.checks | Where-Object { $_.status -eq 'FAIL' })
    $expected += @($report.checks | Where-Object { $_.status -eq 'EXPECTED_FAIL' })
}
$baseUnexpected = if ($null -ne $baseSummary) { @($baseSummary.unexpectedProductFailures) } else { @() }
$unexpected += $baseUnexpected

$infraOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($unexpected.Count -gt 0) { 'FAIL' } elseif ($expected.Count -gt 0) { 'RED_EXPECTED_FAIL' } else { 'PASS' }
$overall = if ($infraOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'RED_EXPECTED_FAIL') { 'RED' } else { 'PASS' }

$layoutByLocale = @{}
if ($null -ne $layout) {
    foreach ($item in @($layout.layouts)) { $layoutByLocale[[string]$item.locale] = $item }
}
$ko = $layoutByLocale['ko']
$en = $layoutByLocale['en']
$qps = $layoutByLocale['qps-long']
$buildPass = $null -ne $build -and $build.result -eq 'Succeeded' -and $build.errors -eq 0 -and $build.executableExists
$smokePass = $null -ne $smoke -and $smoke.result -eq 'PASS' -and $smoke.aliveAtMinimum -and $smoke.respondingAtMinimum
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave9InteractionPromptLayoutGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 9 compact interaction prompt red-first QA gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    branch = ((& git -C $projectRoot branch --show-current).Trim())
    unityVersion = if ($null -ne $layout) { [string]$layout.unityVersion } else { '6000.4.9f1' }
    startedUtc = $started.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    executionPolicy = 'All Unity Editor/build and Windows Player processes ran outside the Codex sandbox; no -noUpm.'
    evidencePolicy = 'Fresh run ID only. Existing Artifacts/Verification and prior ParallelQA evidence were not reused as verdicts or overwritten.'
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infraOverall
    farNearPopupCancel = if ($null -ne $play -and @($play.checks | Where-Object { $_.id -in @('W9P-P01.far_silent','W9P-P02.four_single_near_prompts','W9P-P03.popup_hides_prompt','W9P-P04.cancel_restores_same_target') -and $_.status -eq 'PASS' }).Count -eq 4) { 'PASS 4/4' } else { 'FAIL' }
    promptLayout1280 = if ($null -ne $play -and @($play.checks | Where-Object { $_.id -in @('W9P-P05.compact_below_narration','W9P-P06.world_and_hud_clearance') -and $_.status -eq 'PASS' }).Count -eq 2) { 'PASS' } else { 'EXPECTED_FAIL/RED' }
    koMetrics = $ko
    enMetrics = $en
    qpsLongMetrics = $qps
    actualQpsLong = if ($null -ne $qps -and $qps.actualLocale) { 'PASS' } else { 'NOT_IMPLEMENTED · synthetic 142% stress capture only' }
    syntheticInput = if ($null -ne $play -and @($play.checks | Where-Object { $_.id -eq 'W9P-P08.locale_device_semantics' -and $_.status -eq 'PASS' }).Count -eq 1) { 'PASS keyboard/gamepad same action+target meaning' } else { 'FAIL' }
    physicalGamepad = 'UNVERIFIED'
    physicalGamepadReason = 'No human physical-device actuation evidence was captured; synthetic/shared code-path checks are reported separately.'
    compile = if ($null -ne $baseSummary) { [string]$baseSummary.compile } else { 'FAIL' }
    approachFirstCoreRegression = if ($null -ne $baseSummary) { [string]$baseSummary.approachFirstCoreRegression } else { 'FAIL' }
    wave7BagRegression = if ($null -ne $baseSummary) { [string]$baseSummary.wave7BagRegression } else { 'FAIL' }
    wave6ProgressionRegression = if ($null -ne $baseSummary) { [string]$baseSummary.wave6ProgressionRegression } else { 'FAIL' }
    windowsDevelopmentBuild = if ($buildPass) { 'PASS' } else { 'FAIL' }
    hiddenSmoke = if ($smokePass) { 'PASS' } else { 'FAIL' }
    addressables = if ($null -ne $baseSummary) { [string]$baseSummary.addressables } else { 'FAIL' }
    retainedBaselineProductGaps = if ($null -ne $baseSummary) { @($baseSummary.expectedProductGaps) } else { @() }
    expectedPromptGaps = $expected
    unexpectedRegressions = $unexpected
    infrastructureFailures = $infrastructureFailures.ToArray()
    evidenceRoot = $evidenceRoot
    exactRerun = $exactRerun
    remotePush = $false
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'interaction-prompt-layout-summary.json'), ($summary | ConvertTo-Json -Depth 16) + [Environment]::NewLine, $utf8NoBom)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('Wave 9 compact interaction prompt red-first QA gate')
$lines.Add("Run ID / Baseline: $RunId / $BaselineCommit")
$lines.Add("Overall/Product/Infrastructure: $overall/$productOverall/$infraOverall")
$lines.Add("Far-near-popup-cancel: $($summary.farNearPopupCancel)")
$lines.Add("1280x800 prompt layout: $($summary.promptLayout1280)")
if ($null -ne $ko) { $lines.Add("KO prompt: x=$($ko.promptRect.x) y=$($ko.promptRect.y) w=$($ko.promptRect.width) h=$($ko.promptRect.height) gap=$($ko.narrationGap) playerOverlap=$($ko.overlapsPlayer) pathOverlap=$($ko.overlapsTraversalPath)") }
if ($null -ne $en) { $lines.Add("EN prompt: x=$($en.promptRect.x) y=$($en.promptRect.y) w=$($en.promptRect.width) h=$($en.promptRect.height) gap=$($en.narrationGap) playerOverlap=$($en.overlapsPlayer) pathOverlap=$($en.overlapsTraversalPath)") }
$lines.Add("Actual qps-long: $($summary.actualQpsLong)")
$lines.Add("Synthetic input / physical gamepad: $($summary.syntheticInput) / $($summary.physicalGamepad)")
$lines.Add("Compile/core/Wave7/Wave6: $($summary.compile) / $($summary.approachFirstCoreRegression) / $($summary.wave7BagRegression) / $($summary.wave6ProgressionRegression)")
$lines.Add("Windows build/smoke/addressables: $($summary.windowsDevelopmentBuild) / $($summary.hiddenSmoke) / $($summary.addressables)")
$lines.Add("Expected prompt gaps / unexpected regressions / infrastructure failures: $($expected.Count) / $($unexpected.Count) / $($infrastructureFailures.Count)")
$lines.Add("Physical gamepad: UNVERIFIED")
$lines.Add("Exact rerun: $exactRerun")
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'interaction-prompt-layout-summary.txt'), $lines.ToArray(), $utf8NoBom)

$commandResults = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $started.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    unityVersion = $summary.unityVersion
    exactRerun = $exactRerun
    stages = $stages.ToArray()
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'interaction-prompt-command-results.json'), ($commandResults | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infraOverall"
Write-Output "PROMPT_LAYOUT=$($summary.promptLayout1280)"
Write-Output "PHYSICAL_GAMEPAD=UNVERIFIED"
Write-Output "EVIDENCE=$evidenceRoot"

if ($infraOverall -eq 'FAIL' -or $unexpected.Count -gt 0) { exit 3 }
if ($expected.Count -gt 0) { exit 2 }
exit 0
