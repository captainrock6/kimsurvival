[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '09ae2a6d578eb4dcbf11b9c571f57f640b88d969',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(6, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'Wave 20 requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$wave19Entry = Join-Path $PSScriptRoot 'Invoke-Wave19ResourceConnectionGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $wave19Entry -PathType Leaf)) {
    throw "Wave 19 prerequisite entry point is missing: $wave19Entry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 20 baseline mismatch. Expected $BaselineCommit, observed $head"
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

# Wave 19 recursively owns the fresh Wave 17/18 locks, compiler check,
# Windows Development build, six-second hidden smoke, Addressables, exact
# stable-build identity, and inbound firewall Block evidence.
$wave19Arguments = @(
    '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $wave19Entry,
    '-RunId', $RunId, '-BaselineCommit', $BaselineCommit, '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$wave19Stage = Invoke-HiddenProcess 'fresh-wave19-green-prerequisite' $shellExecutable $wave19Arguments `
    (Join-Path $workRoot 'wave20-wave19-stdout.log') (Join-Path $workRoot 'wave20-wave19-stderr.log')

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}

$stages = New-Object System.Collections.Generic.List[object]
$stages.Add($wave19Stage)

foreach ($definition in @(
    [ordered]@{ name = 'wave20-edit-raft-contracts'; method = 'ParallelQA.Wave20RaftRedFirstGateRunner.RunEditContracts'; play = $false },
    [ordered]@{ name = 'wave20-play-raft-contracts'; method = 'ParallelQA.Wave20RaftRedFirstGateRunner.RunPlayContracts'; play = $true }
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

$wave19Summary = Read-Json (Join-Path $evidenceRoot 'wave19-summary.json')
$wave20Edit = Read-Json (Join-Path $evidenceRoot 'wave20-edit-contracts.json')
$wave20Play = Read-Json (Join-Path $evidenceRoot 'wave20-play-contracts.json')
$wave20Evidence = Read-Json (Join-Path $evidenceRoot 'wave20-play-observation-evidence.json')
$firewall = Read-Json (Join-Path $evidenceRoot 'wave19-windows-firewall-contract.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$smoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$addressables = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$compilePath = Join-Path $evidenceRoot 'compile-result.txt'
$compileText = if (Test-Path -LiteralPath $compilePath -PathType Leaf) { Get-Content -LiteralPath $compilePath -Raw -Encoding UTF8 } else { '' }

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ([int]$wave19Stage.exitCode -ne 0) { $infrastructureFailures.Add("Wave 19 prerequisite exited $($wave19Stage.exitCode)") }
foreach ($stage in @($stages | Select-Object -Skip 1)) {
    if ([int]$stage.exitCode -ne 0) { $infrastructureFailures.Add("$($stage.name) exited $($stage.exitCode)") }
}
if ($null -eq $wave19Summary -or [string]$wave19Summary.overall -ne 'GREEN' -or
    [string]$wave19Summary.productOverall -ne 'PASS' -or [string]$wave19Summary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 19 prerequisite is missing or not GREEN/PASS')
}
if ($null -eq $wave19Summary -or [int]$wave19Summary.wave18GreenLock.passed -ne 23 -or
    [int]$wave19Summary.wave18GreenLock.failed -ne 0) {
    $infrastructureFailures.Add('Wave 18 GREEN lock is not exactly 23/23 PASS')
}
if ($null -eq $wave19Summary -or [int]$wave19Summary.wave19.passed -ne 21 -or
    [int]$wave19Summary.wave19.failed -ne 0) {
    $infrastructureFailures.Add('Wave 19 GREEN lock is not exactly 21/21 PASS')
}
foreach ($report in @($wave20Edit, $wave20Play)) {
    if ($null -eq $report -or [string]$report.infrastructureOverall -ne 'PASS' -or
        [string]$report.runId -ne $RunId -or [string]$report.baselineCommit -ne $BaselineCommit) {
        $infrastructureFailures.Add('a fresh Wave 20 report is missing, identity-mismatched, or infrastructure FAIL')
    }
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
if ($null -eq $firewall -or [string]$firewall.result -ne 'PASS' -or [string]$firewall.runId -ne $RunId -or
    [string]$firewall.baselineCommit -ne $BaselineCommit) {
    $infrastructureFailures.Add('exact stable executable firewall inbound Block contract is not PASS')
}
if ($null -eq $addressables -or [string]$addressables.overall -ne 'PASS') {
    $infrastructureFailures.Add('Addressables post-smoke contract is not PASS')
}
if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log') -PathType Leaf) {
    $infrastructureFailures.Add('raw Windows Player log escaped quarantine')
}
if ($null -eq $wave20Evidence) {
    $infrastructureFailures.Add('Wave 20 Play observation evidence is missing')
}

$wave20Checks = @()
foreach ($report in @($wave20Edit, $wave20Play)) { if ($null -ne $report) { $wave20Checks += @($report.checks) } }
$productPasses = @($wave20Checks | Where-Object { [string]$_.status -eq 'PASS' -and [string]$_.id -notlike 'W20-I*' })
$expectedGaps = @($wave20Checks | Where-Object { [string]$_.status -eq 'EXPECTED_GAP' })
$productFailures = @($wave20Checks | Where-Object { [string]$_.status -eq 'FAIL' })
$unverified = @($wave20Checks | Where-Object { [string]$_.status -eq 'UNVERIFIED' })
$notReady = @($wave20Checks | Where-Object { [string]$_.status -eq 'NOT_READY' })
$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($productFailures.Count -gt 0) { 'FAIL' } elseif ($expectedGaps.Count -gt 0) { 'RED_EXPECTED_GAP' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL' -or $productOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'PASS') { 'GREEN' } else { 'RED' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave20RaftRedFirstGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"
$infrastructureFailureSnapshot = @($infrastructureFailures | ForEach-Object { [string]$_ })
$stageSnapshot = @($stages | ForEach-Object { $_ })
$gapIds = @($expectedGaps | ForEach-Object { [string]$_.id })
$failureIds = @($productFailures | ForEach-Object { [string]$_.id })
$passIds = @($productPasses | ForEach-Object { [string]$_.id })

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 20 independent raft escape RED-first regression gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    powershell = [ordered]@{ edition = $shellEdition; version = $shellVersion; executable = $shellExecutable }
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    wave18GreenLock = if ($null -eq $wave19Summary) { [ordered]@{ passed = 0; total = 23 } } else { $wave19Summary.wave18GreenLock }
    wave19GreenLock = if ($null -eq $wave19Summary) { [ordered]@{ passed = 0; total = 21 } } else { [ordered]@{ passed = [int]$wave19Summary.wave19.passed; total = 21; failed = [int]$wave19Summary.wave19.failed } }
    wave20 = [ordered]@{
        passed = $productPasses.Count
        expectedGaps = $expectedGaps.Count
        failed = $productFailures.Count
        passIds = $passIds
        expectedGapIds = $gapIds
        failureIds = $failureIds
        unverified = $unverified.Count
        notReady = $notReady.Count
    }
    greenLocks = [ordered]@{
        compile = if ($compileText -match 'Compiler errors:\s*0' -and $compileText -match 'Compiler warnings:\s*0') { 'PASS 0/0' } else { 'FAIL' }
        windowsDevelopmentBuild = if ($null -ne $windowsBuild -and [string]$windowsBuild.result -eq 'Succeeded') { 'PASS' } else { 'FAIL' }
        hiddenSmoke = if ($null -ne $smoke -and [string]$smoke.result -eq 'PASS') { "PASS $($smoke.observedSeconds)s" } else { 'FAIL' }
        addressables = if ($null -ne $addressables) { [string]$addressables.overall } else { 'MISSING' }
        firewallInboundBlock = if ($null -ne $firewall) { [string]$firewall.result } else { 'MISSING' }
        rawPlayerLog = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log')) { 'FAIL' } else { 'PASS_QUARANTINED' }
    }
    greenTransitionConditions = @(
        'facility.shore-launch is an actual proximity target with far=0, near=1, compact prompt, popup, and cancel restore',
        'escape.raft naturally completes ordered hull/sail/voyage-supplies with protected part.raft.sailcloth',
        'weather/current allow+reject window and atomic/idempotent cost/failure/cancel/terminal contracts pass',
        'save/restore and ending.escape.raft.open-water album unlock exactly once pass',
        'grant=false, warp=false, skip=false are observed on a real Play interaction trace',
        'ko/en/qps-long near/popup captures are 1280x800 with overflow/offscreen zero and prompt <=512x50',
        'keyboard/mouse and synthetic gamepad target/action semantics match',
        'fresh Wave 19 21/21, Wave 18 23/23, compile/build/smoke/Addressables/firewall remain PASS'
    )
    infrastructureFailures = $infrastructureFailureSnapshot
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    exactRerun = $exactRerun
    stages = $stageSnapshot
    exitCode = $exitCode
}

$summaryPath = Join-Path $evidenceRoot 'wave20-summary.json'
$summaryTextPath = Join-Path $evidenceRoot 'wave20-summary.txt'
[IO.File]::WriteAllText($summaryPath, ($summary | ConvertTo-Json -Depth 20) + [Environment]::NewLine, $utf8NoBom)
$text = @(
    'Wave 20 independent raft escape RED-first regression gate'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Wave 18 GREEN lock: $($summary.wave18GreenLock.passed)/23"
    "Wave 19 GREEN lock: $($summary.wave19GreenLock.passed)/21"
    "Wave 20 PASS/EXPECTED_GAP/FAIL: $($productPasses.Count)/$($expectedGaps.Count)/$($productFailures.Count)"
    "Expected gap IDs: $([string]::Join(', ', $gapIds))"
    "Unexpected failure IDs: $([string]::Join(', ', $failureIds))"
    "Compile/build/smoke/Addressables/firewall: $($summary.greenLocks.compile)/$($summary.greenLocks.windowsDevelopmentBuild)/$($summary.greenLocks.hiddenSmoke)/$($summary.greenLocks.addressables)/$($summary.greenLocks.firewallInboundBlock)"
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
[IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave20-powershell-compatibility.json'), ($compatibility | ConvertTo-Json -Depth 6) + [Environment]::NewLine, $utf8NoBom)
if ($compatibility.result -ne 'PASS') { exit 1 }

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "WAVE18_GREEN=$($summary.wave18GreenLock.passed)/23"
Write-Output "WAVE19_GREEN=$($summary.wave19GreenLock.passed)/21"
Write-Output "WAVE20_PASS=$($productPasses.Count)"
Write-Output "WAVE20_EXPECTED_GAP=$($expectedGaps.Count)"
Write-Output "WAVE20_FAIL=$($productFailures.Count)"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output 'STEAM=NOT_READY'
Write-Output "EVIDENCE=$evidenceRoot"
exit $exitCode
