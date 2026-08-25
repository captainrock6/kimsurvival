[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '5248809018ce934fe328328f194686d8c287734f',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(6, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'The GameJam search-node gate requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$wave20Entry = Join-Path $PSScriptRoot 'Invoke-Wave20RaftRedFirstGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $wave20Entry -PathType Leaf)) {
    throw "Wave 20 prerequisite entry point is missing: $wave20Entry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "GameJam search-node baseline mismatch. Expected $BaselineCommit, observed $head"
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

function Test-Utf8NoBom([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
    $bytes = [IO.File]::ReadAllBytes($Path)
    return $bytes.Length -lt 3 -or -not ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
}

$runStarted = [DateTime]::UtcNow
$shellEdition = if ([string]::IsNullOrWhiteSpace([string]$PSVersionTable.PSEdition)) { 'Desktop' } else { [string]$PSVersionTable.PSEdition }
$shellVersion = [string]$PSVersionTable.PSVersion
$shellExecutable = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
$stages = New-Object System.Collections.Generic.List[object]

# Wave 20 recursively owns the fresh Wave 19/18 locks, compile, Windows x64
# Development build, six-second hidden smoke, Addressables stability, exact
# stable-build SHA/path identity, and inbound firewall Block evidence.
$wave20Arguments = @(
    '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $wave20Entry,
    '-RunId', $RunId, '-BaselineCommit', $BaselineCommit, '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$wave20Stage = Invoke-HiddenProcess 'fresh-wave20-green-prerequisite' $shellExecutable $wave20Arguments `
    (Join-Path $workRoot 'search-node-wave20-stdout.log') (Join-Path $workRoot 'search-node-wave20-stderr.log')
$stages.Add($wave20Stage)

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}

foreach ($definition in @(
    [ordered]@{ name = 'search-node-edit-contracts'; method = 'ParallelQA.GameJamSearchNodeRedFirstGateRunner.RunEditContracts'; play = $false },
    [ordered]@{ name = 'search-node-play-contracts'; method = 'ParallelQA.GameJamSearchNodeRedFirstGateRunner.RunPlayContracts'; play = $true }
)) {
    $logName = $definition.name.Replace('-', '_')
    $arguments = @('-batchmode')
    if ([bool]$definition.play) { $arguments += '-force-d3d11' } else { $arguments += @('-nographics', '-quit') }
    $arguments += @(
        '-projectPath', $projectRoot,
        '-executeMethod', [string]$definition.method,
        '-logFile', (Join-Path $workRoot ("unity-$logName.log"))
    )
    $stage = Invoke-HiddenProcess ([string]$definition.name) $UnityPath $arguments `
        (Join-Path $workRoot ("$logName-stdout.log")) (Join-Path $workRoot ("$logName-stderr.log"))
    $stages.Add($stage)
}

$wave20Summary = Read-Json (Join-Path $evidenceRoot 'wave20-summary.json')
$wave19Play = Read-Json (Join-Path $evidenceRoot 'wave19-play-contracts.json')
$searchEdit = Read-Json (Join-Path $evidenceRoot 'gamejam-search-node-edit-contracts.json')
$searchPlay = Read-Json (Join-Path $evidenceRoot 'gamejam-search-node-play-contracts.json')
$searchEditEvidence = Read-Json (Join-Path $evidenceRoot 'gamejam-search-node-edit-observation-evidence.json')
$searchPlayEvidence = Read-Json (Join-Path $evidenceRoot 'gamejam-search-node-play-observation-evidence.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$smoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$addressables = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$firewall = Read-Json (Join-Path $evidenceRoot 'wave19-windows-firewall-contract.json')
$compilePath = Join-Path $evidenceRoot 'compile-result.txt'
$compileText = if (Test-Path -LiteralPath $compilePath -PathType Leaf) { Get-Content -LiteralPath $compilePath -Raw -Encoding UTF8 } else { '' }

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
foreach ($stage in $stages) {
    if ([int]$stage.exitCode -ne 0) { $infrastructureFailures.Add("$($stage.name) exited $($stage.exitCode)") }
}
if ($null -eq $wave20Summary -or
    [string]$wave20Summary.runId -ne $RunId -or
    [string]$wave20Summary.baselineCommit -ne $BaselineCommit -or
    [string]$wave20Summary.overall -ne 'GREEN' -or
    [string]$wave20Summary.productOverall -ne 'PASS' -or
    [string]$wave20Summary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 20 prerequisite is missing, identity-mismatched, or not GREEN/PASS/PASS')
}
$wave20PrerequisiteStages = if ($null -eq $wave20Summary) { @() } else { @($wave20Summary.stages) }
if ($wave20PrerequisiteStages.Count -eq 0 -or
    @($wave20PrerequisiteStages | Where-Object { [int]$_.exitCode -ne 0 }).Count -gt 0) {
    $infrastructureFailures.Add('fresh Wave 20 prerequisite contains a missing or nonzero child stage')
}
if ($null -eq $wave20Summary -or [int]$wave20Summary.wave20.passed -ne 16 -or
    [int]$wave20Summary.wave20.expectedGaps -ne 0 -or [int]$wave20Summary.wave20.failed -ne 0) {
    $infrastructureFailures.Add('Wave 20 GREEN lock is not exactly 16/16 PASS')
}
$wave19ResourceLock = if ($null -eq $wave19Play) { $null } else { @($wave19Play.checks | Where-Object { [string]$_.id -eq 'W19-P02.resource_nodes_adopted_icons' }) | Select-Object -First 1 }
if ($null -eq $wave19Play -or [string]$wave19Play.infrastructureOverall -ne 'PASS' -or
    [string]$wave19Play.runId -ne $RunId -or [string]$wave19Play.baselineCommit -ne $BaselineCommit) {
    $infrastructureFailures.Add('fresh Wave 19 actual Play report is missing, identity-mismatched, or infrastructure FAIL')
}
$canonicalPlayLock = if ($null -eq $wave19Play) { $null } else { @($wave19Play.checks | Where-Object { [string]$_.id -eq 'W19-R01.current_green_play_regression' }) | Select-Object -First 1 }
if ($null -eq $canonicalPlayLock -or [string]$canonicalPlayLock.status -ne 'PASS') {
    $infrastructureFailures.Add('canonical camp/module/map current GREEN Play verification lock is missing or not PASS')
}
$sameDayCapture = Join-Path $evidenceRoot 'kim-survival-hotfix-expedition-complete-notice-ko-1280x800.png'
if (-not (Test-Path -LiteralPath $sameDayCapture -PathType Leaf)) {
    $infrastructureFailures.Add('fresh same-day map redeparture notice capture is missing')
}
foreach ($report in @($searchEdit, $searchPlay)) {
    if ($null -eq $report -or [string]$report.infrastructureOverall -ne 'PASS' -or
        [string]$report.runId -ne $RunId -or [string]$report.baselineCommit -ne $BaselineCommit) {
        $infrastructureFailures.Add('a fresh search-node report is missing, identity-mismatched, or infrastructure FAIL')
    }
}
if ($null -eq $searchEditEvidence -or $null -eq $searchPlayEvidence) {
    $infrastructureFailures.Add('structured search-node Edit or Play observation evidence is missing')
}
if ($compileText -notmatch 'Compiler errors:\s*0' -or $compileText -notmatch 'Compiler warnings:\s*0') {
    $infrastructureFailures.Add('Unity compile is not 0 errors / 0 warnings')
}
if ($null -eq $windowsBuild -or [string]$windowsBuild.result -ne 'Succeeded' -or
    [string]$windowsBuild.runId -ne $RunId -or [string]$windowsBuild.baselineCommit -ne $BaselineCommit -or
    [string]$windowsBuild.options -ne 'Development' -or [int]$windowsBuild.errors -ne 0 -or [int]$windowsBuild.warnings -ne 0) {
    $infrastructureFailures.Add('Windows x64 Development build identity/result is not PASS')
}
if ($null -eq $smoke -or [string]$smoke.result -ne 'PASS' -or [double]$smoke.observedSeconds -lt $MinimumSmokeSeconds -or
    -not [bool]$smoke.aliveAtMinimum -or -not [bool]$smoke.respondingAtMinimum -or
    [string]$smoke.runId -ne $RunId -or [string]$smoke.baselineCommit -ne $BaselineCommit -or
    -not [bool]$smoke.stableExecutablePath -or -not [bool]$smoke.buildIdentityMatches) {
    $infrastructureFailures.Add("hidden Windows smoke did not PASS for at least $MinimumSmokeSeconds seconds on the exact build")
}
if ($null -eq $addressables -or [string]$addressables.overall -ne 'PASS') {
    $infrastructureFailures.Add('Addressables post-smoke contract is not PASS')
}
if ($null -eq $firewall -or [string]$firewall.result -ne 'PASS' -or
    [string]$firewall.runId -ne $RunId -or [string]$firewall.baselineCommit -ne $BaselineCommit) {
    $infrastructureFailures.Add('exact stable executable firewall inbound Block contract is not PASS')
}
if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log') -PathType Leaf) {
    $infrastructureFailures.Add('raw Windows Player log escaped quarantine')
}

$searchChecks = @()
foreach ($report in @($searchEdit, $searchPlay)) { if ($null -ne $report) { $searchChecks += @($report.checks) } }
$productPasses = @($searchChecks | Where-Object { [string]$_.status -eq 'PASS' -and [string]$_.id -notlike 'GSN-I*' })
$expectedGaps = @($searchChecks | Where-Object { [string]$_.status -eq 'EXPECTED_GAP' })
$productFailures = @($searchChecks | Where-Object { [string]$_.status -eq 'FAIL' })
$wave19ProductFailures = if ($null -eq $wave19Play) { @() } else { @($wave19Play.checks | Where-Object { [string]$_.status -eq 'FAIL' }) }
$allProductFailures = @($productFailures) + @($wave19ProductFailures)
$unverified = @($searchChecks | Where-Object { [string]$_.status -eq 'UNVERIFIED' })
$notReady = @($searchChecks | Where-Object { [string]$_.status -eq 'NOT_READY' })
$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($allProductFailures.Count -gt 0) { 'FAIL' } elseif ($expectedGaps.Count -gt 0) { 'RED_EXPECTED_GAP' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'PASS') { 'GREEN' } else { 'RED' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-GameJamSearchNodeRedFirstGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"
$gapIds = @($expectedGaps | ForEach-Object { [string]$_.id })
$failureIds = @($allProductFailures | ForEach-Object { [string]$_.id })
$passIds = @($productPasses | ForEach-Object { [string]$_.id })

$summary = [ordered]@{
    schemaVersion = 1
    title = 'GameJam searchable resource node RED-first independent regression gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    powershell = [ordered]@{ edition = $shellEdition; version = $shellVersion; executable = $shellExecutable }
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    wave20GreenLock = [ordered]@{
        passed = if ($null -eq $wave20Summary) { 0 } else { [int]$wave20Summary.wave20.passed }
        total = 16
        failed = if ($null -eq $wave20Summary) { -1 } else { [int]$wave20Summary.wave20.failed }
    }
    wave19GreenLock = [ordered]@{
        passed = if ($null -eq $wave20Summary) { 0 } else { [int]$wave20Summary.wave19GreenLock.passed }
        total = 21
        failed = if ($null -eq $wave20Summary) { -1 } else { [int]$wave20Summary.wave19GreenLock.failed }
    }
    wave19ResourceIconDiagnostic = if ($null -eq $wave19ResourceLock) { 'MISSING' } else { [string]$wave19ResourceLock.status + ': ' + [string]$wave19ResourceLock.actual }
    currentGreenLocks = [ordered]@{
        canonicalCampModuleMap = if ($null -ne $canonicalPlayLock -and [string]$canonicalPlayLock.status -eq 'PASS') { 'PASS' } else { 'FAIL' }
        sameDayRedepartureNotice = if (Test-Path -LiteralPath $sameDayCapture -PathType Leaf) { 'PASS' } else { 'FAIL' }
        compile = if ($compileText -match 'Compiler errors:\s*0' -and $compileText -match 'Compiler warnings:\s*0') { 'PASS 0/0' } else { 'FAIL' }
        windowsDevelopmentBuild = if ($null -ne $windowsBuild -and [string]$windowsBuild.result -eq 'Succeeded') { 'PASS' } else { 'FAIL' }
        hiddenSmoke = if ($null -ne $smoke -and [string]$smoke.result -eq 'PASS') { "PASS $($smoke.observedSeconds)s" } else { 'FAIL' }
        addressables = if ($null -ne $addressables) { [string]$addressables.overall } else { 'MISSING' }
        firewallInboundBlock = if ($null -ne $firewall) { [string]$firewall.result } else { 'MISSING' }
        rawPlayerLog = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log')) { 'FAIL' } else { 'PASS_QUARANTINED' }
    }
    searchNode = [ordered]@{
        passed = $productPasses.Count
        expectedGaps = $expectedGaps.Count
        failed = $productFailures.Count
        passIds = $passIds
        expectedGapIds = $gapIds
        failureIds = $failureIds
        unverified = $unverified.Count
        notReady = $notReady.Count
    }
    greenTransitionConditions = @(
        'exactly seven stable region IDs and a finite structured node catalog are public and observed',
        'same seed+region+node structured contents repeat exactly while multiple alternate seeds include a valid variation',
        'Cancel, screen transition, revisit, and save/restore never reroll or lose remaining item IDs/counts',
        'hidden -> revealed-partial -> depleted and take/leave/replace/cancel/duplicate transaction contracts pass',
        'protected parts cannot be discarded/duplicated/double-consumed and part.raft.sailcloth links to escape.raft exactly once',
        'search cost/risk apply once and new hazards pause while the actual compact tray is open',
        'fresh ko/en/qps-long 1280x800 actual captures have zero overflow/offscreen and clear player/walking band',
        'keyboard/mouse and synthetic gamepad structured meanings match; grant/warp/skip remain false',
        'fresh Wave 20 16/16, Wave 19 21/21, canonical camp/module/map, same-day notice, compile/build/smoke/Addressables/firewall remain PASS'
    )
    infrastructureFailures = @($infrastructureFailures | ForEach-Object { [string]$_ })
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    exactRerun = $exactRerun
    stages = @($stages | ForEach-Object { $_ })
    exitCode = $exitCode
}

$summaryPath = Join-Path $evidenceRoot 'gamejam-search-node-summary.json'
$summaryTextPath = Join-Path $evidenceRoot 'gamejam-search-node-summary.txt'
[IO.File]::WriteAllText($summaryPath, ($summary | ConvertTo-Json -Depth 20) + [Environment]::NewLine, $utf8NoBom)
$text = @(
    'GameJam searchable resource node RED-first independent regression gate'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Wave 20 GREEN lock: $($summary.wave20GreenLock.passed)/16"
    "Wave 19 GREEN lock: $($summary.wave19GreenLock.passed)/21"
    "Search node PASS/EXPECTED_GAP/FAIL: $($productPasses.Count)/$($expectedGaps.Count)/$($productFailures.Count)"
    "Wave 19 equivalent resource-icon lock: $($summary.wave19ResourceIconDiagnostic)"
    "Expected gap IDs: $([string]::Join(', ', $gapIds))"
    "Unexpected failure IDs: $([string]::Join(', ', $failureIds))"
    "Camp/module/map and same-day notice: $($summary.currentGreenLocks.canonicalCampModuleMap)/$($summary.currentGreenLocks.sameDayRedepartureNotice)"
    "Compile/build/smoke/Addressables/firewall: $($summary.currentGreenLocks.compile)/$($summary.currentGreenLocks.windowsDevelopmentBuild)/$($summary.currentGreenLocks.hiddenSmoke)/$($summary.currentGreenLocks.addressables)/$($summary.currentGreenLocks.firewallInboundBlock)"
    'Physical gamepad: UNVERIFIED'
    'Steam: NOT_READY'
    "Rerun: $exactRerun"
    "Exit code: $exitCode (0 GREEN, 2 product RED, 1 infrastructure FAIL)"
) -join [Environment]::NewLine
[IO.File]::WriteAllText($summaryTextPath, $text + [Environment]::NewLine, $utf8NoBom)

$compatibility = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    shellEdition = $shellEdition
    shellVersion = $shellVersion
    summaryUtf8NoBom = Test-Utf8NoBom $summaryPath
    rawPlayerLogQuarantined = -not (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log'))
}
$compatibility.result = if ($compatibility.summaryUtf8NoBom -and $compatibility.rawPlayerLogQuarantined) { 'PASS' } else { 'FAIL' }
[IO.File]::WriteAllText((Join-Path $evidenceRoot 'gamejam-search-node-powershell-compatibility.json'), ($compatibility | ConvertTo-Json -Depth 6) + [Environment]::NewLine, $utf8NoBom)
if ($compatibility.result -ne 'PASS') { exit 1 }

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "WAVE20_GREEN=$($summary.wave20GreenLock.passed)/16"
Write-Output "WAVE19_GREEN=$($summary.wave19GreenLock.passed)/21"
Write-Output "SEARCH_NODE_PASS=$($productPasses.Count)"
Write-Output "SEARCH_NODE_EXPECTED_GAP=$($expectedGaps.Count)"
Write-Output "SEARCH_NODE_FAIL=$($productFailures.Count)"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output 'STEAM=NOT_READY'
Write-Output "EVIDENCE=$evidenceRoot"
exit $exitCode
