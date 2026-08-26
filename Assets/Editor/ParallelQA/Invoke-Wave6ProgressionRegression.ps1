[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '473c0824096d02589c46dce92cb1d2264dfb23a4',

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
    throw "Wave 6 baseline mismatch. Expected $BaselineCommit, observed $head"
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
    if ($Graphics) {
        $arguments.Add('-force-d3d11')
    } else {
        $arguments.Add('-nographics')
    }
    if (-not $SelfExiting) {
        $arguments.Add('-quit')
    }
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

& (Join-Path $PSScriptRoot 'Capture-Wave5Preflight.ps1') -RunId $RunId -BaselineCommit $BaselineCommit

$stages = New-Object System.Collections.Generic.List[object]
$stages.Add((Invoke-UnityStage 'compile' 'ParallelQA.ParallelQaRunner.RecordCompilePass' (Join-Path $workRoot 'unity-compile.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave6-edit-contracts' 'ParallelQA.Wave6ProgressionRegressionRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave6-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave6-play-contracts' 'ParallelQA.Wave6ProgressionRegressionRunner.RunPlayContracts' (Join-Path $workRoot 'unity-wave6-play.log') $true $true))
$stages.Add((Invoke-UnityStage 'legacy-full-play' 'ParallelQA.ParallelQaRunner.RunPlayModeVerification' (Join-Path $workRoot 'unity-legacy-full-play.log') $true $true))
$stages.Add((Invoke-UnityStage 'asset-release-contracts' 'ParallelQA.Wave4AssetReleaseGate.RunAssetContracts' (Join-Path $workRoot 'unity-asset-contracts.log') $false $false))
$stages.Add((Invoke-UnityStage 'windows-development-build' 'ParallelQA.Wave4AssetReleaseGate.BuildWindowsDevelopmentPlayer' (Join-Path $workRoot 'unity-windows-build.log') $false $false))

$smokeStarted = [DateTime]::UtcNow
$smokeExit = 0
$smokeError = ''
try {
    & (Join-Path $PSScriptRoot 'Invoke-Wave5WindowsSmoke.ps1') -RunId $RunId -BaselineCommit $BaselineCommit -MinimumSeconds $MinimumSmokeSeconds
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
    unity = $UnityPath
    invokedUtc = [DateTime]::UtcNow.ToString('O')
    executionPolicy = 'Unity Editor/build and Windows Player must run outside the Codex sandbox; no -noUpm.'
    stages = $stages.ToArray()
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave6-command-results.json'), ($commandResults | ConvertTo-Json -Depth 8) + [Environment]::NewLine, $utf8NoBom)

$compileText = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt')) { Get-Content -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt') -Raw } else { '' }
$edit = Read-Json (Join-Path $evidenceRoot 'wave6-edit-contracts.json')
$play = Read-Json (Join-Path $evidenceRoot 'wave6-play-contracts.json')
$asset = Read-Json (Join-Path $evidenceRoot 'asset-contracts.json')
$visual = Read-Json (Join-Path $evidenceRoot 'wave5-current-visual-facts.json')
$preflight = Read-Json (Join-Path $evidenceRoot 'wave5-preflight.json')
$addressBuild = Read-Json (Join-Path $evidenceRoot 'addressables-link-build-contract.json')
$addressSmoke = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$windowsSmoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$steam = Read-Json (Join-Path $evidenceRoot 'steam-readiness.json')

$stageByName = @{}
foreach ($stage in $stages) { $stageByName[$stage.name] = $stage }
$infrastructureFailures = New-Object System.Collections.Generic.List[string]

if ($stageByName['compile'].exitCode -ne 0 -or $compileText -notmatch 'Result:\s+PASS' -or $compileText -notmatch 'Compiler errors:\s+0') {
    $infrastructureFailures.Add('compile stage did not prove PASS/errors 0')
}
if ($null -eq $edit -or $edit.infrastructureOverall -ne 'PASS' -or $stageByName['wave6-edit-contracts'].exitCode -notin @(0, 1)) {
    $infrastructureFailures.Add('Wave 6 Edit contract infrastructure did not complete cleanly')
}
if ($null -eq $play -or $play.infrastructureOverall -ne 'PASS' -or $stageByName['wave6-play-contracts'].exitCode -notin @(0, 1)) {
    $infrastructureFailures.Add('Wave 6 Play contract infrastructure did not complete cleanly')
}
if ($stageByName['legacy-full-play'].exitCode -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $evidenceRoot 'playmode-full-loop.txt'))) {
    $infrastructureFailures.Add('legacy full Play Mode verification did not complete')
}
if ($null -eq $asset -or $stageByName['asset-release-contracts'].exitCode -notin @(0, 1)) {
    $infrastructureFailures.Add('asset/release contract evidence missing or abnormal exit')
}
if ($null -eq $windowsBuild -or $null -eq $windowsSmoke -or $stageByName['windows-development-build'].exitCode -ne 0 -or $smokeExit -ne 0) {
    $infrastructureFailures.Add('Windows build or hidden smoke stage did not complete')
}

$productDefects = New-Object System.Collections.Generic.List[object]
foreach ($report in @($edit, $play)) {
    if ($null -ne $report) {
        foreach ($check in @($report.checks | Where-Object { $_.status -eq 'FAIL' })) {
            $productDefects.Add([ordered]@{
                id = $check.id
                severity = $check.severity
                classification = $check.classification
                expected = $check.expected
                actual = $check.actual
                reproduction = $check.reproduction
                recommendedFiles = $check.recommendedFiles
            })
        }
    }
}

$unexpectedAssetFailures = @()
if ($null -ne $asset) {
    $unexpectedAssetFailures = @($asset.checks | Where-Object { $_.status -eq 'FAIL' })
    foreach ($check in $unexpectedAssetFailures) {
        $productDefects.Add([ordered]@{
            id = $check.id
            severity = $check.severity
            classification = 'PRODUCT_REGRESSION'
            expected = $check.expected
            actual = $check.actual
            reproduction = 'Run ParallelQA.Wave4AssetReleaseGate.RunAssetContracts for the same Wave 6 run ID.'
            recommendedFiles = $check.path
        })
    }
}

$placementNoRegression = $null -ne $visual -and $visual.placement.status -eq 'PASS' -and $visual.placement.targets -eq 4 -and $visual.placement.failures -eq 0
$explorationNoRegression = $null -ne $visual -and $visual.explorationSwimming.status -eq 'PASS' -and $visual.explorationSwimming.targets -eq 4 -and $visual.explorationSwimming.failures -eq 0
$searchTrayNoRegression = $null -ne $visual -and $visual.searchTray.status -eq 'PASS' -and $visual.searchTray.targets -eq 16 -and $visual.searchTray.failures -eq 0
$qpsNoRegression = $null -ne $visual -and $visual.qpsLong.status -eq 'PASS' -and $visual.qpsLong.targets -eq 37 -and $visual.qpsLong.failures -eq 0
$addressLoadPass = $null -ne $preflight -and $preflight.ownershipOverall -eq 'PASS' -and $null -ne $asset -and @($asset.checks | Where-Object { $_.id -eq 'addressables.preflight_stability' -and $_.status -eq 'PASS' }).Count -eq 1
$addressBuildPass = $null -ne $addressBuild -and $addressBuild.overall -eq 'PASS'
$addressSmokePass = $null -ne $addressSmoke -and $addressSmoke.overall -eq 'PASS'
$windowsBuildPass = $null -ne $windowsBuild -and $windowsBuild.result -eq 'Succeeded' -and $windowsBuild.errors -eq 0 -and $windowsBuild.executableExists
$windowsSmokePass = $null -ne $windowsSmoke -and $windowsSmoke.result -eq 'PASS' -and $windowsSmoke.aliveAtMinimum -and $windowsSmoke.respondingAtMinimum
$regressionOverall = if ($placementNoRegression -and $explorationNoRegression -and $searchTrayNoRegression -and $qpsNoRegression -and $addressLoadPass -and $addressBuildPass -and $addressSmokePass -and $windowsBuildPass -and $windowsSmokePass -and $unexpectedAssetFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }

if ($regressionOverall -eq 'FAIL') {
    $productDefects.Add([ordered]@{
        id = 'W6-10.release_regression'
        severity = 'P0'
        classification = 'PRODUCT_REGRESSION'
        expected = 'Addressables ownership, all four current visual markers, Windows build, and hidden smoke remain stable'
        actual = "placement=$placementNoRegression exploration=$explorationNoRegression searchTray=$searchTrayNoRegression qps=$qpsNoRegression address=$addressLoadPass/$addressBuildPass/$addressSmokePass windows=$windowsBuildPass/$windowsSmokePass unexpectedAssetFails=$($unexpectedAssetFailures.Count)"
        reproduction = 'Run Invoke-Wave6ProgressionRegression.ps1 with a fresh run ID.'
        recommendedFiles = 'inspect the failing evidence path before assigning product ownership'
    })
}

$physicalGamepad = if ($null -ne $asset) { $asset.physicalGamepad } else { 'UNVERIFIED' }
if ([string]::IsNullOrWhiteSpace($physicalGamepad)) { $physicalGamepad = 'UNVERIFIED' }
$steamReadiness = if ($null -ne $steam) { $steam.overall } else { 'NOT_READY' }

$summary = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    overall = if ($infrastructureFailures.Count -eq 0 -and $productDefects.Count -eq 0) { 'PASS' } else { 'FAIL' }
    productOverall = if ($productDefects.Count -eq 0) { 'PASS' } else { 'FAIL' }
    infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
    releaseRegressionOverall = $regressionOverall
    compile = if ($compileText -match 'Result:\s+PASS') { 'PASS' } else { 'FAIL' }
    editContracts = if ($null -ne $edit) { $edit.productOverall } else { 'MISSING' }
    playContracts = if ($null -ne $play) { $play.productOverall } else { 'MISSING' }
    normalPlacement = if ($placementNoRegression) { 'PASS 4/4' } else { 'FAIL' }
    explorationSwimming = if ($explorationNoRegression) { 'PASS 4/4' } else { 'FAIL' }
    searchTray = if ($searchTrayNoRegression) { 'PASS 16/16' } else { 'FAIL' }
    qpsLong = if ($qpsNoRegression) { 'PASS 37/37 fresh pity; protected-part trays are Wave B' } else { 'FAIL' }
    addressables = "load=$(if($addressLoadPass){'PASS'}else{'FAIL'}) build=$(if($addressBuildPass){'PASS'}else{'FAIL'}) postSmoke=$(if($addressSmokePass){'PASS'}else{'FAIL'})"
    windowsBuild = if ($windowsBuildPass) { 'PASS' } else { 'FAIL' }
    hiddenSmoke = if ($windowsSmokePass) { 'PASS' } else { 'FAIL' }
    physicalGamepad = $physicalGamepad
    steamReadiness = $steamReadiness
    productDefects = $productDefects.ToArray()
    infrastructureFailures = $infrastructureFailures.ToArray()
    evidenceRoot = $evidenceRoot
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave6-summary.json'), ($summary | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('Wave 6 progression regression summary')
$lines.Add("Run ID: $RunId")
$lines.Add("Baseline: $BaselineCommit")
$lines.Add("Overall: $($summary.overall)")
$lines.Add("Product: $($summary.productOverall)")
$lines.Add("Infrastructure: $($summary.infrastructureOverall)")
$lines.Add("Release regression: $($summary.releaseRegressionOverall)")
$lines.Add("Compile: $($summary.compile)")
$lines.Add("Edit/Play product contracts: $($summary.editContracts)/$($summary.playContracts)")
$lines.Add("Normal placement: $($summary.normalPlacement)")
$lines.Add("Exploration/swimming: $($summary.explorationSwimming)")
$lines.Add("Search tray: $($summary.searchTray)")
$lines.Add("qps-long: $($summary.qpsLong)")
$lines.Add("Addressables: $($summary.addressables)")
$lines.Add("Windows build/smoke: $($summary.windowsBuild)/$($summary.hiddenSmoke)")
$lines.Add("Physical gamepad: $($summary.physicalGamepad)")
$lines.Add("Steam: $($summary.steamReadiness)")
$lines.Add("Product defect count: $($productDefects.Count)")
$lines.Add("Infrastructure failure count: $($infrastructureFailures.Count)")
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave6-summary.txt'), $lines.ToArray(), $utf8NoBom)

Write-Output "OVERALL=$($summary.overall)"
Write-Output "PRODUCT=$($summary.productOverall)"
Write-Output "INFRASTRUCTURE=$($summary.infrastructureOverall)"
Write-Output "RELEASE_REGRESSION=$($summary.releaseRegressionOverall)"
Write-Output "PHYSICAL_GAMEPAD=$($summary.physicalGamepad)"
Write-Output "STEAM=$($summary.steamReadiness)"
Write-Output "EVIDENCE=$evidenceRoot"

if ($infrastructureFailures.Count -gt 0) { exit 3 }
if ($productDefects.Count -gt 0) { exit 2 }
exit 0
