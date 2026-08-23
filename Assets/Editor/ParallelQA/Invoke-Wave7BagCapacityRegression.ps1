[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '19f050a69759e5715d1f8a2eaa72fade72164b4b',

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
    throw "Evidence directory already exists; choose a new run ID: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 7 baseline mismatch. Expected $BaselineCommit, observed $head"
}

New-Item -ItemType Directory -Path $workRoot -Force | Out-Null
$env:KIM_PARALLEL_QA_RUN_ID = $RunId
$env:KIM_PARALLEL_QA_BASELINE = $BaselineCommit

function Quote-Argument([string]$Value) {
    if ($Value -match '[\s"]') {
        return '"' + $Value.Replace('"', '\"') + '"'
    }
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

function Add-Defect(
    [System.Collections.Generic.List[object]]$List,
    [string]$Id,
    [string]$Severity,
    [string]$Classification,
    [string]$Expected,
    [string]$Actual,
    [string]$Reproduction,
    [string]$RecommendedFiles
) {
    $List.Add([ordered]@{
        id = $Id
        severity = $Severity
        classification = $Classification
        expected = $Expected
        actual = $Actual
        reproduction = $Reproduction
        recommendedFiles = $RecommendedFiles
    })
}

$preflightStarted = [DateTime]::UtcNow
$preflightOutput = @(& (Join-Path $PSScriptRoot 'Capture-Wave5Preflight.ps1') -RunId $RunId -BaselineCommit $BaselineCommit 2>&1)
$preflightExit = $LASTEXITCODE

$stages = New-Object System.Collections.Generic.List[object]
$stages.Add([ordered]@{
    name = 'preflight'
    executeMethod = 'Capture-Wave5Preflight.ps1'
    startedUtc = $preflightStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    exitCode = $preflightExit
    log = (Join-Path $evidenceRoot 'wave5-preflight.txt')
    command = "Capture-Wave5Preflight.ps1 -RunId '$RunId' -BaselineCommit '$BaselineCommit'"
    output = [string]::Join(' | ', $preflightOutput)
})
$stages.Add((Invoke-UnityStage 'compile' 'ParallelQA.ParallelQaRunner.RecordCompilePass' (Join-Path $workRoot 'unity-compile.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave7-edit-contracts' 'ParallelQA.Wave7BagCapacityRegressionRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave7-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave7-play-contracts' 'ParallelQA.Wave7BagCapacityRegressionRunner.RunPlayContracts' (Join-Path $workRoot 'unity-wave7-play.log') $true $true))
$stages.Add((Invoke-UnityStage 'wave6-edit-regression' 'ParallelQA.Wave6ProgressionRegressionRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave6-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave6-play-regression' 'ParallelQA.Wave6ProgressionRegressionRunner.RunPlayContracts' (Join-Path $workRoot 'unity-wave6-play.log') $true $true))
$stages.Add((Invoke-UnityStage 'legacy-edit-regression' 'ParallelQA.ParallelQaRunner.RunEditChecks' (Join-Path $workRoot 'unity-legacy-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'legacy-full-play-regression' 'ParallelQA.ParallelQaRunner.RunPlayModeVerification' (Join-Path $workRoot 'unity-legacy-full-play.log') $true $true))
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

$commandResults = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    unityVersionExpected = '6000.4.9f1'
    unityExecutable = $UnityPath
    invokedUtc = [DateTime]::UtcNow.ToString('O')
    executionPolicy = 'Unity Editor/build and Windows Player executed outside the Codex sandbox; no -noUpm.'
    exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave7BagCapacityRegression.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit'"
    stages = $stages.ToArray()
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave7-command-results.json'), ($commandResults | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)

$stageByName = @{}
foreach ($stage in $stages) { $stageByName[$stage.name] = $stage }
$compileText = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt')) { Get-Content -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt') -Raw } else { '' }
$wave7Edit = Read-Json (Join-Path $evidenceRoot 'wave7-edit-contracts.json')
$wave7Play = Read-Json (Join-Path $evidenceRoot 'wave7-play-contracts.json')
$layout = Read-Json (Join-Path $evidenceRoot 'wave7-layout-metrics.json')
$wave6Edit = Read-Json (Join-Path $evidenceRoot 'wave6-edit-contracts.json')
$wave6Play = Read-Json (Join-Path $evidenceRoot 'wave6-play-contracts.json')
$asset = Read-Json (Join-Path $evidenceRoot 'asset-contracts.json')
$visual = Read-Json (Join-Path $evidenceRoot 'wave5-current-visual-facts.json')
$preflight = Read-Json (Join-Path $evidenceRoot 'wave5-preflight.json')
$addressBuild = Read-Json (Join-Path $evidenceRoot 'addressables-link-build-contract.json')
$addressSmoke = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$windowsSmoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$steam = Read-Json (Join-Path $evidenceRoot 'steam-readiness.json')
$legacyEditText = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'edit-checks.txt')) { Get-Content -LiteralPath (Join-Path $evidenceRoot 'edit-checks.txt') -Raw } else { '' }
$legacyPlayText = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'playmode-full-loop.txt')) { Get-Content -LiteralPath (Join-Path $evidenceRoot 'playmode-full-loop.txt') -Raw } else { '' }

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($preflightExit -ne 0 -or $null -eq $preflight) { $infrastructureFailures.Add('preflight did not complete') }
if ($stageByName['compile'].exitCode -ne 0 -or $compileText -notmatch 'Result:\s+PASS' -or $compileText -notmatch 'Compiler errors:\s+0') {
    $infrastructureFailures.Add('compile stage did not prove PASS/errors 0')
}
foreach ($pair in @(
    @('wave7-edit-contracts', $wave7Edit),
    @('wave7-play-contracts', $wave7Play),
    @('wave6-edit-regression', $wave6Edit),
    @('wave6-play-regression', $wave6Play)
)) {
    $stage = $stageByName[$pair[0]]
    $report = $pair[1]
    if ($null -eq $report -or $report.infrastructureOverall -ne 'PASS' -or $stage.exitCode -notin @(0, 1)) {
        $infrastructureFailures.Add("$($pair[0]) infrastructure did not complete cleanly")
    }
}
if ($null -eq $layout) { $infrastructureFailures.Add('Wave 7 layout report missing') }
if ($legacyEditText -notmatch 'Overall:\s+PASS' -or $stageByName['legacy-edit-regression'].exitCode -notin @(0, 1)) {
    $infrastructureFailures.Add('legacy Edit regression did not complete with parseable evidence')
}
if ([string]::IsNullOrWhiteSpace($legacyPlayText) -or $stageByName['legacy-full-play-regression'].exitCode -notin @(0, 1)) {
    $infrastructureFailures.Add('legacy full Play regression evidence missing')
}
if ($null -eq $asset -or $stageByName['asset-release-contracts'].exitCode -notin @(0, 1)) {
    $infrastructureFailures.Add('asset/release contract evidence missing or abnormal exit')
}
if ($null -eq $windowsBuild -or $null -eq $windowsSmoke -or $stageByName['windows-development-build'].exitCode -ne 0 -or $smokeExit -ne 0) {
    $infrastructureFailures.Add('Windows build or hidden smoke did not complete')
}

$wave7Findings = New-Object System.Collections.Generic.List[object]
foreach ($report in @($wave7Edit, $wave7Play)) {
    if ($null -eq $report) { continue }
    foreach ($check in @($report.checks | Where-Object { $_.status -in @('FAIL', 'NOT_IMPLEMENTED') })) {
        Add-Defect $wave7Findings $check.id $check.severity $check.classification $check.expected $check.actual $check.reproduction $check.recommendedFiles
    }
}

$regressionDefects = New-Object System.Collections.Generic.List[object]
foreach ($report in @($wave6Edit, $wave6Play)) {
    if ($null -eq $report) { continue }
    foreach ($check in @($report.checks | Where-Object { $_.status -in @('FAIL', 'NOT_IMPLEMENTED') })) {
        Add-Defect $regressionDefects $check.id $check.severity 'PRODUCT_REGRESSION' $check.expected $check.actual $check.reproduction $check.recommendedFiles
    }
}
if ($legacyEditText -notmatch 'Overall:\s+PASS') {
    Add-Defect $regressionDefects 'W7-10.legacy_edit' 'P0' 'PRODUCT_REGRESSION' 'Legacy deterministic Edit Check PASS' 'legacy Edit Check not PASS' 'Run ParallelQA.ParallelQaRunner.RunEditChecks.' 'inspect edit-checks.txt before assigning product ownership'
}

$placementPass = $null -ne $visual -and $visual.placement.status -eq 'PASS' -and $visual.placement.targets -eq 24 -and $visual.placement.failures -eq 0
$explorationPass = $null -ne $visual -and $visual.explorationSwimming.status -eq 'PASS' -and $visual.explorationSwimming.targets -eq 10 -and $visual.explorationSwimming.failures -eq 0
$qpsPass = $null -ne $visual -and $visual.qpsLong.status -eq 'PASS' -and $visual.qpsLong.targets -eq 10 -and $visual.qpsLong.failures -eq 0
$assetPass = $null -ne $asset -and $asset.overall -eq 'PASS'
$addressLoadPass = $null -ne $preflight -and $preflight.ownershipOverall -eq 'PASS' -and $null -ne $asset -and @($asset.checks | Where-Object { $_.id -eq 'addressables.preflight_stability' -and $_.status -eq 'PASS' }).Count -eq 1
$addressBuildPass = $null -ne $addressBuild -and $addressBuild.overall -eq 'PASS'
$addressSmokePass = $null -ne $addressSmoke -and $addressSmoke.overall -eq 'PASS'
$windowsBuildPass = $null -ne $windowsBuild -and $windowsBuild.result -eq 'Succeeded' -and $windowsBuild.errors -eq 0 -and $windowsBuild.executableExists
$windowsSmokePass = $null -ne $windowsSmoke -and $windowsSmoke.result -eq 'PASS' -and $windowsSmoke.aliveAtMinimum -and $windowsSmoke.respondingAtMinimum

if (-not $placementPass) { Add-Defect $regressionDefects 'W7-10.placement' 'P0' 'PRODUCT_REGRESSION' 'ko/en placement 24/24 PASS' 'fresh visual fact did not pass 24/24' 'Run the legacy full Play regression.' 'inspect wave3-visual-gate.txt' }
if (-not $explorationPass) { Add-Defect $regressionDefects 'W7-10.swimming' 'P0' 'PRODUCT_REGRESSION' 'exploration/swimming 10/10 PASS' 'fresh visual fact did not pass 10/10' 'Run the legacy full Play regression.' 'inspect wave3-visual-gate.txt' }
if (-not $qpsPass) { Add-Defect $regressionDefects 'W7-10.qps_long' 'P1' 'PRODUCT_REGRESSION' 'qps-long 10/10 PASS' 'fresh visual fact did not pass 10/10' 'Run the legacy full Play regression.' 'inspect wave3-visual-gate.txt' }
if (-not $assetPass) { Add-Defect $regressionDefects 'W7-10.asset_contracts' 'P0' 'PRODUCT_REGRESSION' 'asset/release contracts PASS' 'asset contract overall is not PASS' 'Run Wave4AssetReleaseGate.RunAssetContracts.' 'inspect asset-contracts.json' }
if (-not ($addressLoadPass -and $addressBuildPass -and $addressSmokePass)) { Add-Defect $regressionDefects 'W7-10.addressables' 'P0' 'PRODUCT_REGRESSION' 'Addressables load/build/post-smoke PASS' "load=$addressLoadPass build=$addressBuildPass postSmoke=$addressSmokePass" 'Run the full Wave 7 command with a fresh run ID.' 'inspect Addressables contract JSON files' }
if (-not ($windowsBuildPass -and $windowsSmokePass)) { Add-Defect $regressionDefects 'W7-10.windows' 'P0' 'PRODUCT_REGRESSION' 'Windows Development build and hidden smoke PASS' "build=$windowsBuildPass smoke=$windowsSmokePass" 'Run the full Wave 7 command with a fresh run ID.' 'inspect windows-development-build.json and windows-hidden-smoke.json' }

$physicalGamepad = 'UNVERIFIED'
$steamReadiness = if ($null -ne $steam) { [string]$steam.overall } else { 'NOT_READY' }
if ([string]::IsNullOrWhiteSpace($steamReadiness) -or $steamReadiness -eq 'READY') {
    $steamReadiness = if ($null -eq $steam) { 'NOT_READY' } else { [string]$steam.overall }
}
$warnings = if ($null -ne $windowsBuild) { [int]$windowsBuild.warnings } else { -1 }
$existingRegressionOverall = if ($regressionDefects.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($wave7Findings.Count -eq 0 -and $regressionDefects.Count -eq 0) { 'PASS' } else { 'FAIL' }
$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$overall = if ($productOverall -eq 'PASS' -and $infrastructureOverall -eq 'PASS') { 'PASS' } else { 'FAIL' }

$summary = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    redFirst = if ($wave7Findings.Count -gt 0 -and $infrastructureOverall -eq 'PASS') { 'EXPECTED_RED_PRODUCT_GAP_REPRODUCED' } elseif ($wave7Findings.Count -eq 0) { 'GREEN_AFTER_IMPLEMENTATION' } else { 'INCONCLUSIVE_INFRASTRUCTURE_FAILURE' }
    compile = if ($compileText -match 'Result:\s+PASS' -and $compileText -match 'Compiler errors:\s+0') { 'PASS 0 errors' } else { 'FAIL' }
    wave7Edit = if ($null -ne $wave7Edit) { $wave7Edit.productOverall } else { 'MISSING' }
    wave7Play = if ($null -ne $wave7Play) { $wave7Play.productOverall } else { 'MISSING' }
    wave7Layout = if ($null -ne $layout) { $layout.overall } else { 'MISSING' }
    existingRegressionOverall = $existingRegressionOverall
    wave6Progression = if ($null -ne $wave6Edit -and $null -ne $wave6Play -and $wave6Edit.productOverall -eq 'PASS' -and $wave6Play.productOverall -eq 'PASS') { 'PASS' } else { 'FAIL' }
    placement = if ($placementPass) { 'PASS 24/24' } else { 'FAIL' }
    explorationSwimming = if ($explorationPass) { 'PASS 10/10' } else { 'FAIL' }
    qpsLong = if ($qpsPass) { 'PASS 10/10' } else { 'FAIL' }
    addressables = "load=$(if($addressLoadPass){'PASS'}else{'FAIL'}) build=$(if($addressBuildPass){'PASS'}else{'FAIL'}) postSmoke=$(if($addressSmokePass){'PASS'}else{'FAIL'})"
    windowsDevelopmentBuild = if ($windowsBuildPass) { 'PASS' } else { 'FAIL' }
    windowsBuildWarnings = $warnings
    hiddenSmoke = if ($windowsSmokePass) { 'PASS' } else { 'FAIL' }
    physicalGamepad = $physicalGamepad
    physicalGamepadReason = 'No physical-device human actuation was performed; synthetic/code-path automation cannot upgrade this status.'
    steamReadiness = $steamReadiness
    steamReadyClaim = $false
    wave7Findings = $wave7Findings.ToArray()
    regressionDefects = $regressionDefects.ToArray()
    infrastructureFailures = $infrastructureFailures.ToArray()
    evidenceRoot = $evidenceRoot
    exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave7BagCapacityRegression.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit'"
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave7-summary.json'), ($summary | ConvertTo-Json -Depth 12) + [Environment]::NewLine, $utf8NoBom)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('Wave 7 bag capacity regression summary')
$lines.Add("Run ID: $RunId")
$lines.Add("Baseline: $BaselineCommit")
$lines.Add("Overall: $($summary.overall)")
$lines.Add("Red-first: $($summary.redFirst)")
$lines.Add("Product/Infrastructure: $($summary.productOverall)/$($summary.infrastructureOverall)")
$lines.Add("Compile: $($summary.compile)")
$lines.Add("Wave 7 Edit/Play/Layout: $($summary.wave7Edit)/$($summary.wave7Play)/$($summary.wave7Layout)")
$lines.Add("Existing regressions: $($summary.existingRegressionOverall)")
$lines.Add("Wave 6 progression: $($summary.wave6Progression)")
$lines.Add("Placement: $($summary.placement)")
$lines.Add("Exploration/swimming: $($summary.explorationSwimming)")
$lines.Add("qps-long: $($summary.qpsLong)")
$lines.Add("Addressables: $($summary.addressables)")
$lines.Add("Windows build/smoke: $($summary.windowsDevelopmentBuild)/$($summary.hiddenSmoke) · warnings=$warnings")
$lines.Add("Physical gamepad: $physicalGamepad")
$lines.Add("Steam: $steamReadiness · READY claim=false")
$lines.Add("Wave 7 finding count: $($wave7Findings.Count)")
$lines.Add("Regression defect count: $($regressionDefects.Count)")
$lines.Add("Infrastructure failure count: $($infrastructureFailures.Count)")
$lines.Add("Exact rerun: $($summary.exactRerun)")
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave7-summary.txt'), $lines.ToArray(), $utf8NoBom)

Write-Output "OVERALL=$overall"
Write-Output "RED_FIRST=$($summary.redFirst)"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "EXISTING_REGRESSIONS=$existingRegressionOverall"
Write-Output "PHYSICAL_GAMEPAD=$physicalGamepad"
Write-Output "STEAM=$steamReadiness"
Write-Output "EVIDENCE=$evidenceRoot"

if ($infrastructureFailures.Count -gt 0) { exit 3 }
if ($wave7Findings.Count -gt 0 -or $regressionDefects.Count -gt 0) { exit 2 }
exit 0
