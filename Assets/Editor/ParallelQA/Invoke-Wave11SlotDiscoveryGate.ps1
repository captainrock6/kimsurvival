[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '72ec967a9009635fbeccbc758563183a67a4b311',

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
    throw "Wave 11 baseline mismatch. Expected $BaselineCommit, observed $head"
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
$stages.Add((Invoke-UnityStage 'wave11-slot-edit' 'ParallelQA.Wave11SlotDiscoveryGateRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave11-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave11-slot-play' 'ParallelQA.Wave11SlotDiscoveryGateRunner.RunPlayContracts' (Join-Path $workRoot 'unity-wave11-play.log') $true $true))
$stages.Add((Invoke-UnityStage 'asset-release-contracts' 'ParallelQA.Wave4AssetReleaseGate.RunAssetContracts' (Join-Path $workRoot 'unity-asset-contracts.log') $false $false))
$stages.Add((Invoke-UnityStage 'windows-development-build' 'ParallelQA.Wave4AssetReleaseGate.BuildWindowsDevelopmentPlayer' (Join-Path $workRoot 'unity-windows-build.log') $false $false))

$smokeStarted = [DateTime]::UtcNow
$smokeExit = 0
$smokeError = ''
try {
    $global:LASTEXITCODE = 0
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
    title = 'Wave 11 direct connection-slot RED-first gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    unityVersionExpected = '6000.4.9f1'
    unityExecutable = $UnityPath
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    executionPolicy = 'All Unity Editor/build and Windows Player processes ran outside the Codex sandbox; no -noUpm.'
    evidencePolicy = 'Fresh run ID only. storage.planning is an auxiliary regression and cannot satisfy direct slot discovery.'
    exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave11SlotDiscoveryGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"
    stages = $stages.ToArray()
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave11-command-results.json'), ($commandResults | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)

$stageByName = @{}
foreach ($stage in $stages) { $stageByName[$stage.name] = $stage }
$preflight = Read-Json (Join-Path $evidenceRoot 'wave5-preflight.json')
$compileText = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt')) { Get-Content -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt') -Raw } else { '' }
$edit = Read-Json (Join-Path $evidenceRoot 'wave11-slot-edit-contracts.json')
$play = Read-Json (Join-Path $evidenceRoot 'wave11-slot-play-contracts.json')
$asset = Read-Json (Join-Path $evidenceRoot 'asset-contracts.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$windowsSmoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$addressBuild = Read-Json (Join-Path $evidenceRoot 'addressables-link-build-contract.json')
$addressSmoke = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$steam = Read-Json (Join-Path $evidenceRoot 'steam-readiness.json')

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($preflightExit -ne 0 -or $null -eq $preflight -or $preflight.ownershipOverall -ne 'PASS') {
    $infrastructureFailures.Add('preflight/Addressables ownership did not pass')
}
if ($stageByName['compile'].exitCode -ne 0 -or $compileText -notmatch 'Result:\s+PASS' -or $compileText -notmatch 'Compiler errors:\s+0') {
    $infrastructureFailures.Add('Unity compile did not prove PASS with zero errors')
}
foreach ($name in @('wave11-slot-edit','wave11-slot-play')) {
    $report = if ($name -eq 'wave11-slot-edit') { $edit } else { $play }
    if ($stageByName[$name].exitCode -ne 0 -or $null -eq $report -or $report.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add("$name did not complete with infrastructure PASS")
    }
}

$assetFailures = if ($null -ne $asset) { @($asset.checks | Where-Object { $_.status -eq 'FAIL' }) } else { @() }
$assetCorePass = $null -ne $asset -and
    @($assetFailures | Where-Object { $_.id -notlike 'visual.current_*' }).Count -eq 0 -and
    @($asset.checks | Where-Object { $_.status -eq 'PASS' }).Count -gt 0
if ($null -eq $asset -or $stageByName['asset-release-contracts'].exitCode -notin @(0, 1) -or -not $assetCorePass) {
    $infrastructureFailures.Add('asset/release core contracts did not complete')
}

$buildPass = $null -ne $windowsBuild -and $windowsBuild.result -eq 'Succeeded' -and $windowsBuild.errors -eq 0 -and $windowsBuild.executableExists
$smokePass = $null -ne $windowsSmoke -and $windowsSmoke.result -eq 'PASS' -and $windowsSmoke.aliveAtMinimum -and $windowsSmoke.respondingAtMinimum
$addressLoadPass = $null -ne $preflight -and $preflight.ownershipOverall -eq 'PASS' -and $null -ne $asset -and
    @($asset.checks | Where-Object { $_.id -eq 'addressables.preflight_stability' -and $_.status -eq 'PASS' }).Count -eq 1
$addressPass = $addressLoadPass -and $null -ne $addressBuild -and $addressBuild.overall -eq 'PASS' -and $null -ne $addressSmoke -and $addressSmoke.overall -eq 'PASS'
if (-not $buildPass) { $infrastructureFailures.Add('Windows x64 Development build did not pass') }
if (-not $smokePass -or $smokeExit -ne 0) { $infrastructureFailures.Add('hidden Windows Player smoke did not pass') }
if (-not $addressPass) { $infrastructureFailures.Add('Addressables load/build/post-smoke contract did not pass') }

$allChecks = @()
if ($null -ne $edit) { $allChecks += @($edit.checks) }
if ($null -ne $play) { $allChecks += @($play.checks) }
$unexpectedProductFailures = @($allChecks | Where-Object { $_.status -eq 'FAIL' })
$expectedProductGaps = @($allChecks | Where-Object { $_.status -eq 'EXPECTED_FAIL' -and $_.classification -eq 'PRODUCT_EXPECTED_GAP' })
$directChecks = @($allChecks | Where-Object { $_.id -in @('W11-E02.direct_slot_runtime_surface','W11-E06.ko_en_qps_direct_action','W11-P01.direct_slot_discovery_popup_and_cancel','W11-P02.direct_slot_1280_layout') })
$directPass = $directChecks.Count -eq 4 -and @($directChecks | Where-Object { $_.status -ne 'PASS' }).Count -eq 0
$storagePass = @($allChecks | Where-Object { $_.id -eq 'W11-P03.storage_planning_auxiliary_regression' -and $_.status -eq 'PASS' }).Count -eq 1
$regressionPass = @($allChecks | Where-Object { $_.id -eq 'W11-P04.full_survival_regression' -and $_.status -eq 'PASS' }).Count -eq 1

$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($unexpectedProductFailures.Count -gt 0) { 'FAIL' } elseif ($expectedProductGaps.Count -gt 0) { 'RED_EXPECTED_FAIL' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'RED_EXPECTED_FAIL') { 'RED' } else { 'PASS' }
$directSlotGate = if ($directPass) { 'GREEN' } else { 'RED_EXPECTED_FAIL' }
$steamReadiness = if ($null -ne $steam -and -not [string]::IsNullOrWhiteSpace([string]$steam.overall)) { [string]$steam.overall } else { 'NOT_READY' }
$warnings = if ($null -ne $windowsBuild) { [int]$windowsBuild.warnings } else { -1 }

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 11 direct connection-slot RED-first gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    directSlotGate = $directSlotGate
    directChecksPassed = @($directChecks | Where-Object { $_.status -eq 'PASS' }).Count
    directChecksTotal = $directChecks.Count
    storagePlanningAuxiliary = if ($storagePass) { 'PASS_NOT_SUBSTITUTE' } else { 'FAIL' }
    fullSurvivalRegression = if ($regressionPass) { 'PASS' } else { 'FAIL' }
    compile = if ($compileText -match 'Result:\s+PASS' -and $compileText -match 'Compiler errors:\s+0') { 'PASS 0 errors' } else { 'FAIL' }
    windowsDevelopmentBuild = if ($buildPass) { 'PASS' } else { 'FAIL' }
    windowsBuildWarnings = $warnings
    hiddenSmoke = if ($smokePass) { 'PASS' } else { 'FAIL' }
    addressables = if ($addressPass) { 'PASS load/build/post-smoke' } else { 'FAIL' }
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = $steamReadiness
    steamReadyClaim = $false
    expectedProductGaps = @($expectedProductGaps | ForEach-Object {
        [ordered]@{ id = [string]$_.id; severity = [string]$_.severity; actual = [string]$_.actual; reproduction = [string]$_.reproduction; recommendedFiles = [string]$_.recommendedFiles }
    })
    unexpectedProductFailures = @($unexpectedProductFailures | ForEach-Object {
        [ordered]@{ id = [string]$_.id; severity = [string]$_.severity; actual = [string]$_.actual; reproduction = [string]$_.reproduction; recommendedFiles = [string]$_.recommendedFiles }
    })
    infrastructureFailures = $infrastructureFailures.ToArray()
    exactRerun = $commandResults.exactRerun
    evidenceRoot = $evidenceRoot
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave11-summary.json'), ($summary | ConvertTo-Json -Depth 12) + [Environment]::NewLine, $utf8NoBom)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('Wave 11 direct connection-slot RED-first gate')
$lines.Add("Run ID: $RunId")
$lines.Add("Baseline: $BaselineCommit")
$lines.Add("Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall")
$lines.Add("Direct slot gate: $directSlotGate ($(@($directChecks | Where-Object { $_.status -eq 'PASS' }).Count)/$($directChecks.Count))")
$lines.Add("storage.planning auxiliary: $($summary.storagePlanningAuxiliary)")
$lines.Add("Full survival regression: $($summary.fullSurvivalRegression)")
$lines.Add("Compile: $($summary.compile)")
$lines.Add("Windows build: $($summary.windowsDevelopmentBuild), warnings=$warnings")
$lines.Add("Hidden smoke: $($summary.hiddenSmoke)")
$lines.Add("Addressables: $($summary.addressables)")
$lines.Add('Physical gamepad: UNVERIFIED')
$lines.Add("Steam: $steamReadiness (READY claim=false)")
$lines.Add("Expected product gaps: $($expectedProductGaps.Count)")
$lines.Add("Unexpected product failures: $($unexpectedProductFailures.Count)")
$lines.Add("Infrastructure failures: $($infrastructureFailures.Count)")
$lines.Add("Evidence: $evidenceRoot")
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave11-summary.txt'), $lines, $utf8NoBom)

Write-Output "DIRECT_SLOT_GATE=$directSlotGate"
Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "STORAGE_PLANNING=$($summary.storagePlanningAuxiliary)"
Write-Output "PHYSICAL_GAMEPAD=UNVERIFIED"
Write-Output "EVIDENCE=$evidenceRoot"

if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { exit 1 }
if ($productOverall -eq 'RED_EXPECTED_FAIL') { exit 2 }
exit 0
