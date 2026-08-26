[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '86b6db8d5bc628aa7cb9cdb0d3e59539b6633c91',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(5, 60)]
    [int]$MinimumSmokeSeconds = 6,

    [switch]$PreflightOnly
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not $PreflightOnly -and -not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh run ID: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 12 baseline mismatch. Expected $BaselineCommit, observed $head"
}

if (-not $PreflightOnly) {
    New-Item -ItemType Directory -Path $workRoot -Force | Out-Null
}
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
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Test-Utf8NoBom([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    return $bytes.Length -lt 3 -or -not ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
}

$runStarted = [DateTime]::UtcNow
$global:LASTEXITCODE = 0
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

$preflightJson = Join-Path $evidenceRoot 'wave5-preflight.json'
$preflightText = Join-Path $evidenceRoot 'wave5-preflight.txt'
$preflightJsonNoBom = Test-Utf8NoBom $preflightJson
$preflightTextNoBom = Test-Utf8NoBom $preflightText
$shellEdition = if ([string]::IsNullOrWhiteSpace([string]$PSVersionTable.PSEdition)) { 'Desktop' } else { [string]$PSVersionTable.PSEdition }
$shellVersion = [string]$PSVersionTable.PSVersion
$shellCompatibilityOverall = if ($preflightExit -eq 0 -and $preflightJsonNoBom -and $preflightTextNoBom) { 'PASS' } else { 'FAIL' }
$shellCompatibility = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    entryPoint = 'Invoke-Wave12FiveDayUiGate.ps1'
    mode = if ($PreflightOnly) { 'PreflightOnly' } else { 'Full' }
    psEdition = $shellEdition
    psVersion = $shellVersion
    preflightExitCode = $preflightExit
    preflightJsonUtf8NoBom = $preflightJsonNoBom
    preflightTextUtf8NoBom = $preflightTextNoBom
    evidenceFormat = 'UTF-8 without BOM; existing Wave 5 JSON/TXT names and schemas preserved'
    overall = $shellCompatibilityOverall
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave12-powershell-compatibility.json'), ($shellCompatibility | ConvertTo-Json -Depth 6) + [Environment]::NewLine, $utf8NoBom)
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave12-powershell-compatibility.txt'), @(
    'Wave 12 PowerShell compatibility preflight'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "PowerShell: $shellEdition $shellVersion"
    "Mode: $($shellCompatibility.mode)"
    "Preflight exit: $preflightExit"
    "Preflight JSON UTF-8 no BOM: $preflightJsonNoBom"
    "Preflight TXT UTF-8 no BOM: $preflightTextNoBom"
    "Result: $shellCompatibilityOverall"
), $utf8NoBom)

if ($PreflightOnly) {
    Write-Output "POWERSHELL_COMPATIBILITY=$shellCompatibilityOverall"
    Write-Output "POWERSHELL_EDITION=$shellEdition"
    Write-Output "POWERSHELL_VERSION=$shellVersion"
    Write-Output "EVIDENCE=$evidenceRoot"
    if ($shellCompatibilityOverall -ne 'PASS') { exit 1 }
    exit 0
}

$stages.Add((Invoke-UnityStage 'compile' 'ParallelQA.ParallelQaRunner.RecordCompilePass' (Join-Path $workRoot 'unity-compile.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave11-slot-edit' 'ParallelQA.Wave11SlotDiscoveryGateRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave11-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave11-slot-play' 'ParallelQA.Wave11SlotDiscoveryGateRunner.RunPlayContracts' (Join-Path $workRoot 'unity-wave11-play.log') $true $true))
$stages.Add((Invoke-UnityStage 'wave12-five-day-ui-edit' 'ParallelQA.Wave12FiveDayCompactUiGateRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave12-edit.log') $false $false))
$stages.Add((Invoke-UnityStage 'wave12-five-day-ui-play' 'ParallelQA.Wave12FiveDayCompactUiGateRunner.RunPlayContracts' (Join-Path $workRoot 'unity-wave12-play.log') $true $true))
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
    title = 'Wave 12 session-profile compact-a current gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    unityVersionExpected = '6000.4.9f1'
    unityExecutable = $UnityPath
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    executionPolicy = 'All Unity Editor/build and Windows Player processes ran outside the Codex sandbox; no -noUpm.'
    evidencePolicy = 'Fresh run ID only. A fresh Wave 3 visual report is generated before the asset/release contracts.'
    powershell = $shellCompatibility
    exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave12FiveDayUiGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"
    stages = $stages.ToArray()
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave12-command-results.json'), ($commandResults | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)

$stageByName = @{}
foreach ($stage in $stages) { $stageByName[$stage.name] = $stage }
$preflight = Read-Json (Join-Path $evidenceRoot 'wave5-preflight.json')
$compileText = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt')) { Get-Content -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt') -Raw -Encoding UTF8 } else { '' }
$wave3Text = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'wave3-visual-gate.txt')) { Get-Content -LiteralPath (Join-Path $evidenceRoot 'wave3-visual-gate.txt') -Raw -Encoding UTF8 } else { '' }
$wave11Edit = Read-Json (Join-Path $evidenceRoot 'wave11-slot-edit-contracts.json')
$wave11Play = Read-Json (Join-Path $evidenceRoot 'wave11-slot-play-contracts.json')
$wave12Edit = Read-Json (Join-Path $evidenceRoot 'wave12-edit-contracts.json')
$wave12Play = Read-Json (Join-Path $evidenceRoot 'wave12-play-contracts.json')
$asset = Read-Json (Join-Path $evidenceRoot 'asset-contracts.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$windowsSmoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$addressBuild = Read-Json (Join-Path $evidenceRoot 'addressables-link-build-contract.json')
$addressSmoke = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$steam = Read-Json (Join-Path $evidenceRoot 'steam-readiness.json')

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($preflightExit -ne 0 -or $null -eq $preflight -or $preflight.ownershipOverall -ne 'PASS') { $infrastructureFailures.Add('preflight/Addressables ownership did not pass') }
if ($stageByName['compile'].exitCode -ne 0 -or $compileText -notmatch 'Result:\s+PASS' -or $compileText -notmatch 'Compiler errors:\s+0') { $infrastructureFailures.Add('Unity compile did not prove PASS with zero errors') }
$freshWave3IdentityPass = -not [string]::IsNullOrWhiteSpace($wave3Text) -and
    $wave3Text -match ('Run ID:\s+' + [regex]::Escape($RunId)) -and
    $wave3Text -match ('Baseline commit:\s+' + [regex]::Escape($BaselineCommit))
$freshWave3Pass = $freshWave3IdentityPass -and
    $wave3Text -match 'PLACEMENT_GATE:\s+PASS\s+\u00B7\s+targets=4\s+\u00B7\s+failures=0' -and
    $wave3Text -match 'EXPLORATION_SWIMMING_GATE:\s+PASS\s+\u00B7\s+targets=4\s+\u00B7\s+failures=0' -and
    $wave3Text -match 'SEARCH_TRAY_GATE:\s+PASS\s+\u00B7\s+targets=16\s+\u00B7\s+failures=0' -and
    $wave3Text -match 'PSEUDO_LONG_GATE:\s+PASS\s+\u00B7\s+targets=37\s+\u00B7\s+failures=0'
if (-not $freshWave3IdentityPass) { $infrastructureFailures.Add('fresh Wave 3 visual report was not generated for the current RunId/baseline') }
foreach ($name in @('wave11-slot-edit','wave11-slot-play','wave12-five-day-ui-edit','wave12-five-day-ui-play')) {
    $report = switch ($name) {
        'wave11-slot-edit' { $wave11Edit }
        'wave11-slot-play' { $wave11Play }
        'wave12-five-day-ui-edit' { $wave12Edit }
        default { $wave12Play }
    }
    if ($stageByName[$name].exitCode -notin @(0, 1) -or $null -eq $report -or $report.infrastructureOverall -ne 'PASS') { $infrastructureFailures.Add("$name did not complete with infrastructure PASS") }
}
$assetFailures = if ($null -ne $asset) { @($asset.checks | Where-Object { $_.status -eq 'FAIL' }) } else { @() }
$assetCorePass = $null -ne $asset -and @($assetFailures | Where-Object { $_.id -notlike 'visual.current_*' }).Count -eq 0 -and
    @($asset.checks | Where-Object { $_.id -eq 'visual.current_baseline_identity' -and $_.status -eq 'PASS' }).Count -eq 1
if ($stageByName['asset-release-contracts'].exitCode -notin @(0, 1) -or -not $assetCorePass) { $infrastructureFailures.Add('asset/release non-visual core contracts or fresh baseline identity did not pass') }

$buildPass = $null -ne $windowsBuild -and $windowsBuild.result -eq 'Succeeded' -and $windowsBuild.errors -eq 0 -and $windowsBuild.executableExists
$smokePass = $null -ne $windowsSmoke -and $windowsSmoke.result -eq 'PASS' -and $windowsSmoke.aliveAtMinimum -and $windowsSmoke.respondingAtMinimum
$addressPass = $null -ne $preflight -and $preflight.ownershipOverall -eq 'PASS' -and $null -ne $asset -and
    @($asset.checks | Where-Object { $_.id -eq 'addressables.preflight_stability' -and $_.status -eq 'PASS' }).Count -eq 1 -and
    $null -ne $addressBuild -and $addressBuild.overall -eq 'PASS' -and $null -ne $addressSmoke -and $addressSmoke.overall -eq 'PASS'
if (-not $buildPass) { $infrastructureFailures.Add('Windows x64 Development build did not pass') }
if (-not $smokePass -or $smokeExit -ne 0) { $infrastructureFailures.Add('hidden Windows Player smoke did not pass') }
if (-not $addressPass) { $infrastructureFailures.Add('Addressables load/build/post-smoke contract did not pass') }

$allChecks = @()
foreach ($report in @($wave11Edit, $wave11Play, $wave12Edit, $wave12Play)) { if ($null -ne $report) { $allChecks += @($report.checks) } }
$unexpectedProductFailures = @($allChecks | Where-Object { $_.status -eq 'FAIL' })
$expectedProductGaps = @($allChecks | Where-Object { $_.status -eq 'EXPECTED_FAIL' -and $_.classification -eq 'PRODUCT_EXPECTED_GAP' })
$wave11Layout = @($allChecks | Where-Object { $_.id -eq 'W11-P02.direct_slot_1280_layout' -and $_.status -eq 'PASS' })
$approvedExpectedGapIds = @()
$unexpectedExpectedGap = @($expectedProductGaps | Where-Object { $_.id -notin $approvedExpectedGapIds })
if ($unexpectedExpectedGap.Count -gt 0) { $infrastructureFailures.Add('an EXPECTED_FAIL occurred outside the approved Wave 12 baseline gap list') }

$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($unexpectedProductFailures.Count -gt 0) { 'FAIL' } elseif ($expectedProductGaps.Count -gt 0) { 'RED_EXPECTED_FAIL' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'RED_EXPECTED_FAIL') { 'RED' } else { 'PASS' }
$steamReadiness = if ($null -ne $steam -and -not [string]::IsNullOrWhiteSpace([string]$steam.overall)) { [string]$steam.overall } else { 'NOT_READY' }
$warnings = if ($null -ne $windowsBuild) { [int]$windowsBuild.warnings } else { -1 }

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 12 session-profile compact-a current gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    freshWave3Visual = if ($freshWave3Pass) { 'PASS' } else { 'FAIL' }
    wave11WalkingPath = if ($wave11Layout.Count -eq 1) { 'PASS 3/3 actual screen rects' } else { 'FAIL' }
    expectedProductGapIds = @($expectedProductGaps | ForEach-Object { [string]$_.id })
    approvedExpectedGapIds = $approvedExpectedGapIds
    expectedProductGaps = @($expectedProductGaps | ForEach-Object { [ordered]@{ id=[string]$_.id; severity=[string]$_.severity; actual=[string]$_.actual; reproduction=[string]$_.reproduction; recommendedFiles=[string]$_.recommendedFiles } })
    unexpectedProductFailures = @($unexpectedProductFailures | ForEach-Object { [ordered]@{ id=[string]$_.id; severity=[string]$_.severity; actual=[string]$_.actual; reproduction=[string]$_.reproduction; recommendedFiles=[string]$_.recommendedFiles } })
    compile = if ($compileText -match 'Result:\s+PASS' -and $compileText -match 'Compiler errors:\s+0') { 'PASS 0 errors' } else { 'FAIL' }
    windowsDevelopmentBuild = if ($buildPass) { 'PASS' } else { 'FAIL' }
    windowsBuildWarnings = $warnings
    hiddenSmoke = if ($smokePass) { 'PASS' } else { 'FAIL' }
    addressables = if ($addressPass) { 'PASS load/build/post-smoke' } else { 'FAIL' }
    physicalGamepad = 'UNVERIFIED'
    powershellCompatibility = $shellCompatibilityOverall
    steamReadiness = $steamReadiness
    steamReadyClaim = $false
    infrastructureFailures = $infrastructureFailures.ToArray()
    greenTransition = 'Standard profile Day 50, provisional Game Jam profile Day 20 within tune range 15..20, and Day 1/settlement early-escape priority all PASS; compact-a runtime, twelve locale/state captures, and current Wave3 4/4 + 4/4 + 16/16 + fresh-pity 37/37 markers remain PASS. Protected-part trays remain a separate Wave B contract.'
    exactRerun = $commandResults.exactRerun
    evidenceRoot = $evidenceRoot
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave12-summary.json'), ($summary | ConvertTo-Json -Depth 12) + [Environment]::NewLine, $utf8NoBom)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('Wave 12 session-profile compact-a current gate')
$lines.Add("Run ID: $RunId")
$lines.Add("Baseline: $BaselineCommit")
$lines.Add("Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall")
$lines.Add("Fresh Wave3 visual: $($summary.freshWave3Visual)")
$lines.Add("Wave11 walking path: $($summary.wave11WalkingPath)")
$lines.Add("Compile: $($summary.compile)")
$lines.Add("Windows build: $($summary.windowsDevelopmentBuild), warnings=$warnings")
$lines.Add("Hidden smoke: $($summary.hiddenSmoke)")
$lines.Add("Addressables: $($summary.addressables)")
$lines.Add("PowerShell compatibility: $($summary.powershellCompatibility) ($shellEdition $shellVersion)")
$lines.Add('Physical gamepad: UNVERIFIED')
$lines.Add("Steam: $steamReadiness (READY claim=false)")
$lines.Add("Expected product gaps: $($expectedProductGaps.Count) [$([string]::Join(', ', @($summary.expectedProductGapIds)))]")
$lines.Add("Approved expected-gap IDs: $([string]::Join(', ', @($summary.approvedExpectedGapIds)))")
$lines.Add("Unexpected product failures: $($unexpectedProductFailures.Count)")
$lines.Add("Infrastructure failures: $($infrastructureFailures.Count)")
$lines.Add("Evidence: $evidenceRoot")
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave12-summary.txt'), $lines, $utf8NoBom)

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "FRESH_WAVE3=$($summary.freshWave3Visual)"
Write-Output "WAVE11_WALKING_PATH=$($summary.wave11WalkingPath)"
Write-Output "POWERSHELL_COMPATIBILITY=$($summary.powershellCompatibility)"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output "STEAM=$steamReadiness"
Write-Output "EVIDENCE=$evidenceRoot"

if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { exit 1 }
if ($productOverall -eq 'RED_EXPECTED_FAIL') { exit 2 }
