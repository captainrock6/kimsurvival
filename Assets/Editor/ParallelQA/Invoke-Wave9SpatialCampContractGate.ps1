[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = 'd088cbdf021765a811ed88af9b22b58db49b917c',

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
    throw "Wave 9 baseline mismatch. Expected $BaselineCommit, observed $head"
}

New-Item -ItemType Directory -Path $workRoot -Force | Out-Null
$env:KIM_PARALLEL_QA_RUN_ID = $RunId
$env:KIM_PARALLEL_QA_BASELINE = $BaselineCommit

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
    $started = [DateTime]::UtcNow
    $process = Start-Process -FilePath $UnityPath -ArgumentList $argumentLine -WindowStyle Hidden -Wait -PassThru
    return [ordered]@{
        name = $Name
        executeMethod = $ExecuteMethod
        startedUtc = $started.ToString('O')
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

function Add-ProductFinding([System.Collections.Generic.List[object]]$List, [object]$Check) {
    $List.Add([ordered]@{
        id = [string]$Check.id
        severity = [string]$Check.severity
        classification = [string]$Check.classification
        expected = [string]$Check.expected
        actual = [string]$Check.actual
        reproduction = [string]$Check.reproduction
        recommendedFiles = [string]$Check.recommendedFiles
    })
}

$runStarted = [DateTime]::UtcNow
$preflightOutput = @(& (Join-Path $PSScriptRoot 'Capture-Wave5Preflight.ps1') -RunId $RunId -BaselineCommit $BaselineCommit 2>&1)
$preflightExit = $LASTEXITCODE
$stages = New-Object System.Collections.Generic.List[object]
$stages.Add([ordered]@{
    name = 'preflight'
    executeMethod = 'Capture-Wave5Preflight.ps1'
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    exitCode = $preflightExit
    log = (Join-Path $evidenceRoot 'wave5-preflight.txt')
    command = "Capture-Wave5Preflight.ps1 -RunId '$RunId' -BaselineCommit '$BaselineCommit'"
    output = [string]::Join(' | ', $preflightOutput)
})

$stages.Add((Invoke-UnityStage 'compile' 'ParallelQA.ParallelQaRunner.RecordCompilePass' (Join-Path $workRoot 'unity-compile.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave9-edit-contracts' 'ParallelQA.Wave9SpatialCampContractGateRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave9-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave9-play-contracts' 'ParallelQA.Wave9SpatialCampContractGateRunner.RunPlayContracts' (Join-Path $workRoot 'unity-wave9-play.log') $true $true))
$stages.Add((Invoke-UnityStage 'wave7-edit-regression' 'ParallelQA.Wave7BagCapacityRegressionRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave7-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave7-play-regression' 'ParallelQA.Wave7BagCapacityRegressionRunner.RunPlayContracts' (Join-Path $workRoot 'unity-wave7-play.log') $true $true))
$stages.Add((Invoke-UnityStage 'wave6-edit-regression' 'ParallelQA.Wave6ProgressionRegressionRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave6-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave6-play-regression' 'ParallelQA.Wave6ProgressionRegressionRunner.RunPlayContracts' (Join-Path $workRoot 'unity-wave6-play.log') $true $true))
$stages.Add((Invoke-UnityStage 'asset-release-contracts' 'ParallelQA.Wave4AssetReleaseGate.RunAssetContracts' (Join-Path $workRoot 'unity-asset-contracts.log') $false $false))
$stages.Add((Invoke-UnityStage 'windows-development-build' 'ParallelQA.Wave4AssetReleaseGate.BuildWindowsDevelopmentPlayer' (Join-Path $workRoot 'unity-windows-build.log') $false $false))

$smokeStarted = [DateTime]::UtcNow
$smokeExit = 0
$smokeError = ''
try {
    & (Join-Path $PSScriptRoot 'Invoke-Wave5WindowsSmoke.ps1') -RunId $RunId -BaselineCommit $BaselineCommit -MinimumSeconds $MinimumSmokeSeconds
    $smokeExit = $LASTEXITCODE
} catch {
    $smokeExit = 1
    $smokeError = $_.Exception.Message
}
$stages.Add([ordered]@{
    name = 'windows-hidden-smoke'
    executeMethod = 'Invoke-Wave5WindowsSmoke.ps1'
    startedUtc = $smokeStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    exitCode = $smokeExit
    log = (Join-Path $evidenceRoot 'windows-player.log')
    command = "Invoke-Wave5WindowsSmoke.ps1 -RunId '$RunId' -BaselineCommit '$BaselineCommit' -MinimumSeconds $MinimumSmokeSeconds"
    error = $smokeError
})

$legacySource = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Editor\ParallelQA\ParallelQaRunner.cs') -Raw
$legacyIsolation = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    status = 'ISOLATED_BY_DESIGN'
    classification = 'STALE_TEST_ASSUMPTION'
    productRollbackSignal = $false
    reason = 'The legacy Edit assertion searches for the pre-proximity signal delegate, and the legacy natural Play loop submits workbench/signal dashboard buttons without first approaching their spatial targets.'
    oldSignalDelegateAssertionPresent = $legacySource.Contains('delegate { session.TryUpgradeSignal(); RefreshAll(); }')
    distantSignalSubmitPresent = $legacySource.Contains('Submit(GetButton(prototype, "signalButton"));')
    replacement = 'Wave9SpatialCampContractGateRunner uses the current approach-first RunAutomatedVerification plus independent near/interact/popup contracts.'
    exactLegacyReproduction = 'ParallelQA.ParallelQaRunner.RunEditChecks / RunPlayModeVerification (informational only; not a Wave 9 product rollback gate)'
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave9-legacy-harness-isolation.json'), ($legacyIsolation | ConvertTo-Json -Depth 8) + [Environment]::NewLine, $utf8NoBom)
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave9-legacy-harness-isolation.txt'), @(
    'Wave 9 legacy dashboard harness isolation'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    'Status: ISOLATED_BY_DESIGN'
    'Classification: STALE_TEST_ASSUMPTION; product rollback signal=false'
    "Reason: $($legacyIsolation.reason)"
    "Replacement: $($legacyIsolation.replacement)"
), $utf8NoBom)

$commandResults = [ordered]@{
    schemaVersion = 1
    title = 'Wave 9 spatial camp red-first QA contract gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    unityVersionExpected = '6000.4.9f1'
    unityExecutable = $UnityPath
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    executionPolicy = 'All Unity Editor/build and Windows Player processes ran outside the Codex sandbox; no -noUpm.'
    evidencePolicy = 'Fresh run ID only. Prior Wave evidence was audited for test design and was not reused as a verdict.'
    legacyPolicy = 'Old distant global-dashboard checks are isolated, not counted as product regression and not a reason to restore the dashboard.'
    exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave9SpatialCampContractGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit'"
    stages = $stages.ToArray()
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave9-command-results.json'), ($commandResults | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)

$stageByName = @{}
foreach ($stage in $stages) { $stageByName[$stage.name] = $stage }
$compileText = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt')) { Get-Content -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt') -Raw } else { '' }
$preflight = Read-Json (Join-Path $evidenceRoot 'wave5-preflight.json')
$wave9Edit = Read-Json (Join-Path $evidenceRoot 'wave9-edit-contracts.json')
$wave9Play = Read-Json (Join-Path $evidenceRoot 'wave9-play-contracts.json')
$wave9Evidence = Read-Json (Join-Path $evidenceRoot 'wave9-spatial-play-evidence.json')
$wave7Edit = Read-Json (Join-Path $evidenceRoot 'wave7-edit-contracts.json')
$wave7Play = Read-Json (Join-Path $evidenceRoot 'wave7-play-contracts.json')
$wave6Edit = Read-Json (Join-Path $evidenceRoot 'wave6-edit-contracts.json')
$wave6Play = Read-Json (Join-Path $evidenceRoot 'wave6-play-contracts.json')
$asset = Read-Json (Join-Path $evidenceRoot 'asset-contracts.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$windowsSmoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$addressBuild = Read-Json (Join-Path $evidenceRoot 'addressables-link-build-contract.json')
$addressSmoke = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$steam = Read-Json (Join-Path $evidenceRoot 'steam-readiness.json')

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($preflightExit -ne 0 -or $null -eq $preflight -or $preflight.ownershipOverall -ne 'PASS') { $infrastructureFailures.Add('preflight/Addressables ownership did not pass') }
if ($stageByName['compile'].exitCode -ne 0 -or $compileText -notmatch 'Result:\s+PASS' -or $compileText -notmatch 'Compiler errors:\s+0') { $infrastructureFailures.Add('Unity compile did not prove PASS with zero errors') }
foreach ($name in @('wave9-edit-contracts','wave9-play-contracts')) {
    $report = if ($name -eq 'wave9-edit-contracts') { $wave9Edit } else { $wave9Play }
    if ($stageByName[$name].exitCode -ne 0 -or $null -eq $report -or $report.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add("$name did not complete with infrastructure PASS")
    }
}
foreach ($name in @('wave7-edit-regression','wave7-play-regression','wave6-edit-regression','wave6-play-regression')) {
    $report = switch ($name) {
        'wave7-edit-regression' { $wave7Edit }
        'wave7-play-regression' { $wave7Play }
        'wave6-edit-regression' { $wave6Edit }
        default { $wave6Play }
    }
    if ($stageByName[$name].exitCode -ne 0 -or $null -eq $report -or $report.productOverall -ne 'PASS' -or $report.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add("$name did not prove product/infrastructure PASS")
    }
}
$assetFailedChecks = if ($null -ne $asset) { @($asset.checks | Where-Object { $_.status -eq 'FAIL' }) } else { @() }
$assetCorePass = $null -ne $asset -and
    @($assetFailedChecks | Where-Object { $_.id -notlike 'visual.current_*' }).Count -eq 0 -and
    @($asset.checks | Where-Object { $_.status -eq 'PASS' }).Count -gt 0
$staleWave4VisualBridge = $null -ne $asset -and $assetFailedChecks.Count -gt 0 -and
    @($assetFailedChecks | Where-Object { $_.id -notlike 'visual.current_*' }).Count -eq 0
if ($null -eq $asset -or $stageByName['asset-release-contracts'].exitCode -notin @(0, 1) -or -not $assetCorePass) {
    $infrastructureFailures.Add('asset/release core contracts did not complete; failures were not limited to the stale Wave 4 visual-evidence adapter')
}

$buildPass = $null -ne $windowsBuild -and $windowsBuild.result -eq 'Succeeded' -and $windowsBuild.errors -eq 0 -and $windowsBuild.executableExists
$smokePass = $null -ne $windowsSmoke -and $windowsSmoke.result -eq 'PASS' -and $windowsSmoke.aliveAtMinimum -and $windowsSmoke.respondingAtMinimum
$addressLoadPass = $null -ne $preflight -and $preflight.ownershipOverall -eq 'PASS' -and $null -ne $asset -and
    @($asset.checks | Where-Object { $_.id -eq 'addressables.preflight_stability' -and $_.status -eq 'PASS' }).Count -eq 1
$addressPass = $addressLoadPass -and $null -ne $addressBuild -and $addressBuild.overall -eq 'PASS' -and $null -ne $addressSmoke -and $addressSmoke.overall -eq 'PASS'
if (-not $buildPass) { $infrastructureFailures.Add('Windows x64 Development build did not pass') }
if (-not $smokePass -or $smokeExit -ne 0) { $infrastructureFailures.Add('hidden Windows Player smoke did not pass') }
if (-not $addressPass) { $infrastructureFailures.Add('Addressables load/build/post-smoke contract did not pass') }

$productFindings = New-Object System.Collections.Generic.List[object]
foreach ($report in @($wave9Edit, $wave9Play)) {
    if ($null -eq $report) { continue }
    foreach ($check in @($report.checks | Where-Object { $_.status -in @('EXPECTED_FAIL','FAIL') })) {
        Add-ProductFinding $productFindings $check
    }
}

$unexpectedProductFailures = @($productFindings | Where-Object { $_.classification -ne 'PRODUCT_EXPECTED_GAP' })
$expectedProductGaps = @($productFindings | Where-Object { $_.classification -eq 'PRODUCT_EXPECTED_GAP' })
$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($unexpectedProductFailures.Count -gt 0) { 'FAIL' } elseif ($expectedProductGaps.Count -gt 0) { 'RED_EXPECTED_FAIL' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'RED_EXPECTED_FAIL') { 'RED' } else { 'PASS' }
$steamReadiness = if ($null -ne $steam -and -not [string]::IsNullOrWhiteSpace([string]$steam.overall)) { [string]$steam.overall } else { 'NOT_READY' }
$warnings = if ($null -ne $windowsBuild) { [int]$windowsBuild.warnings } else { -1 }

$wave4VisualIsolation = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    status = if ($staleWave4VisualBridge) { 'ISOLATED_BY_DESIGN' } elseif ($assetCorePass) { 'NOT_TRIGGERED' } else { 'INFRA_FAIL' }
    classification = 'STALE_EVIDENCE_ADAPTER'
    productRollbackSignal = $false
    assetCoreChecksPassed = if ($null -ne $asset) { @($asset.checks | Where-Object { $_.status -eq 'PASS' }).Count } else { 0 }
    isolatedFailures = @($assetFailedChecks | ForEach-Object { [ordered]@{ id = $_.id; actual = $_.actual } })
    reason = 'Wave4AssetReleaseGate requires wave5-current-visual-facts.json and the historical 671c4e9 baseline identity. Wave 9 deliberately generates fresh spatial-camp evidence under wave9-spatial-play-evidence.json instead of fabricating that legacy file.'
    replacementEvidence = @('wave9-spatial-play-evidence.json','wave9-play-contracts.json','wave9-ko-normal-camp-1280x800.png','wave9-en-normal-camp-1280x800.png')
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave9-wave4-visual-bridge-isolation.json'), ($wave4VisualIsolation | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 9 spatial camp red-first QA contract gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    compile = if ($compileText -match 'Result:\s+PASS' -and $compileText -match 'Compiler errors:\s+0') { 'PASS 0 errors' } else { 'FAIL' }
    spatialCampContract = if ($null -ne $wave9Play) { [string]$wave9Play.productOverall } else { 'MISSING' }
    normalCampDashboard = if ($null -ne $wave9Evidence -and -not $wave9Evidence.globalCampActionsActive) { 'PASS absent' } else { 'EXPECTED_FAIL present' }
    largeBagPanel = if ($null -ne $wave9Evidence -and -not $wave9Evidence.largeBagPanelActive) { 'PASS absent/compact' } else { 'EXPECTED_FAIL large persistent panel' }
    farState = if ($null -ne $wave9Evidence -and $wave9Evidence.farPromptCount -eq 0 -and $wave9Evidence.farPopupCount -eq 0) { 'PASS silent' } else { 'EXPECTED_FAIL' }
    nearPromptAndPopup = if ($null -ne $wave9Play -and @($wave9Play.checks | Where-Object { $_.id -in @('W9-P03.single_near_target_prompt','W9-P04.popup_only_after_interact') -and $_.status -eq 'PASS' }).Count -eq 2) { 'PASS' } else { 'EXPECTED_FAIL/RED' }
    modalAndReturn = if ($null -ne $wave9Play -and @($wave9Play.checks | Where-Object { $_.id -in @('W9-P05.modal_movement_lock','W9-P06.confirm_cancel_return') -and $_.status -eq 'PASS' }).Count -eq 2) { 'PASS' } else { 'EXPECTED_FAIL/RED' }
    moduleExpansion = if ($null -ne $wave9Edit -and @($wave9Edit.checks | Where-Object { $_.id -like 'W9-M*' -and $_.status -eq 'PASS' }).Count -eq 4) { 'PASS' } else { 'EXPECTED_FAIL/RED' }
    approachFirstCoreRegression = if ($null -ne $wave9Play -and @($wave9Play.checks | Where-Object { $_.id -eq 'W9-I04.approach_first_full_regression' -and $_.status -eq 'PASS' }).Count -eq 1) { 'PASS' } else { 'FAIL' }
    wave7BagRegression = if ($null -ne $wave7Edit -and $wave7Edit.productOverall -eq 'PASS' -and $null -ne $wave7Play -and $wave7Play.productOverall -eq 'PASS') { 'PASS' } else { 'FAIL' }
    wave6ProgressionRegression = if ($null -ne $wave6Edit -and $wave6Edit.productOverall -eq 'PASS' -and $null -ne $wave6Play -and $wave6Play.productOverall -eq 'PASS') { 'PASS' } else { 'FAIL' }
    legacyDashboardHarness = 'ISOLATED_BY_DESIGN · stale distant-button assumptions are not a product rollback signal'
    koEn1280Captures = if ($null -ne $wave9Evidence -and @($wave9Evidence.screenshots).Count -ge 4) { 'PASS fresh captures generated' } else { 'FAIL' }
    assetReleaseCore = if ($assetCorePass) { "PASS $(@($asset.checks | Where-Object { $_.status -eq 'PASS' }).Count) checks" } else { 'FAIL' }
    wave4VisualEvidenceAdapter = if ($staleWave4VisualBridge) { 'ISOLATED_BY_DESIGN · historical Wave 5 filename/baseline coupling' } elseif ($assetCorePass) { 'PASS' } else { 'FAIL' }
    windowsDevelopmentBuild = if ($buildPass) { 'PASS' } else { 'FAIL' }
    windowsBuildWarnings = $warnings
    hiddenSmoke = if ($smokePass) { 'PASS' } else { 'FAIL' }
    addressables = if ($addressPass) { 'PASS load/build/post-smoke' } else { 'FAIL' }
    physicalGamepad = 'UNVERIFIED'
    physicalGamepadReason = 'Synthetic/shared code paths were automated; no physical-device human actuation was captured.'
    steamReadiness = $steamReadiness
    steamReadyClaim = $false
    expectedProductGaps = $expectedProductGaps
    unexpectedProductFailures = $unexpectedProductFailures
    infrastructureFailures = $infrastructureFailures.ToArray()
    screenshots = if ($null -ne $wave9Evidence) { @($wave9Evidence.screenshots) } else { @() }
    evidenceRoot = $evidenceRoot
    exactRerun = $commandResults.exactRerun
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave9-summary.json'), ($summary | ConvertTo-Json -Depth 14) + [Environment]::NewLine, $utf8NoBom)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('Wave 9 spatial camp red-first QA contract gate')
$lines.Add("Run ID: $RunId")
$lines.Add("Baseline: $BaselineCommit")
$lines.Add("Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall")
$lines.Add("Compile: $($summary.compile)")
$lines.Add("Normal camp dashboard / large bag: $($summary.normalCampDashboard) / $($summary.largeBagPanel)")
$lines.Add("Far / near+popup / modal+return: $($summary.farState) / $($summary.nearPromptAndPopup) / $($summary.modalAndReturn)")
$lines.Add("Module expansion: $($summary.moduleExpansion)")
$lines.Add("Approach-first core / Wave7 bag / Wave6 progression: $($summary.approachFirstCoreRegression) / $($summary.wave7BagRegression) / $($summary.wave6ProgressionRegression)")
$lines.Add("Legacy dashboard harness: $($summary.legacyDashboardHarness)")
$lines.Add("KO/EN 1280x800 captures: $($summary.koEn1280Captures)")
$lines.Add("Asset core / Wave4 visual adapter: $($summary.assetReleaseCore) / $($summary.wave4VisualEvidenceAdapter)")
$lines.Add("Windows build/smoke/addressables: $($summary.windowsDevelopmentBuild)/$($summary.hiddenSmoke)/$($summary.addressables) · warnings=$warnings")
$lines.Add("Physical gamepad: $($summary.physicalGamepad)")
$lines.Add("Steam: $steamReadiness · READY claim=false")
$lines.Add("Expected product gaps: $($expectedProductGaps.Count)")
$lines.Add("Unexpected product failures: $($unexpectedProductFailures.Count)")
$lines.Add("Infrastructure failures: $($infrastructureFailures.Count)")
$lines.Add("Exact rerun: $($summary.exactRerun)")
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave9-summary.txt'), $lines.ToArray(), $utf8NoBom)

$metadata = @(
    'Wave 9 run metadata'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Branch: $((& git -C $projectRoot branch --show-current).Trim())"
    'Unity: 6000.4.9f1'
    "Started/completed UTC: $($runStarted.ToString('O')) / $([DateTime]::UtcNow.ToString('O'))"
    "OS: $([System.Environment]::OSVersion.VersionString)"
    'Execution: sandbox_permissions=require_escalated; no -noUpm'
    'Evidence policy: fresh run only; prior PASS/FAIL files not reused as verdicts'
    'Physical gamepad: UNVERIFIED'
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Exact rerun: $($summary.exactRerun)"
)
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'run-metadata.txt'), $metadata, $utf8NoBom)

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "EXPECTED_PRODUCT_GAPS=$($expectedProductGaps.Count)"
Write-Output "PHYSICAL_GAMEPAD=$($summary.physicalGamepad)"
Write-Output "STEAM=$steamReadiness"
Write-Output "EVIDENCE=$evidenceRoot"

if ($infrastructureFailures.Count -gt 0 -or $unexpectedProductFailures.Count -gt 0) { exit 3 }
if ($expectedProductGaps.Count -gt 0) { exit 2 }
exit 0
