[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '00e0d5a9df597ab4a9f54bff665291f367d40c92',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(6, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'The GameJam Wave B gate requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$searchNodeEntry = Join-Path $PSScriptRoot 'Invoke-GameJamSearchNodeRedFirstGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $searchNodeEntry -PathType Leaf)) {
    throw "GameJam search-node prerequisite entry point is missing: $searchNodeEntry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "GameJam Wave B baseline mismatch. Expected $BaselineCommit, observed $head"
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

# The fresh GSN prerequisite recursively owns Wave 20/19, compilation,
# Windows x64 Development build, hidden smoke, Addressables, and firewall.
$searchArguments = @(
    '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $searchNodeEntry,
    '-RunId', $RunId, '-BaselineCommit', $BaselineCommit, '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$searchStage = Invoke-HiddenProcess 'fresh-gsn-green-prerequisite' $shellExecutable $searchArguments `
    (Join-Path $workRoot 'wave-b-gsn-stdout.log') (Join-Path $workRoot 'wave-b-gsn-stderr.log')
$stages.Add($searchStage)

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}

foreach ($definition in @(
    [ordered]@{ name = 'wave-b-edit-contracts'; method = 'ParallelQA.GameJamWaveBRedFirstGateRunner.RunEditContracts'; play = $false },
    [ordered]@{ name = 'wave-b-play-contracts'; method = 'ParallelQA.GameJamWaveBRedFirstGateRunner.RunPlayContracts'; play = $true }
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

$gsnSummary = Read-Json (Join-Path $evidenceRoot 'gamejam-search-node-summary.json')
$waveBEdit = Read-Json (Join-Path $evidenceRoot 'gamejam-wave-b-edit-contracts.json')
$waveBPlay = Read-Json (Join-Path $evidenceRoot 'gamejam-wave-b-play-contracts.json')
$waveBEditEvidence = Read-Json (Join-Path $evidenceRoot 'gamejam-wave-b-edit-observation-evidence.json')
$waveBPlayEvidence = Read-Json (Join-Path $evidenceRoot 'gamejam-wave-b-play-observation-evidence.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$smoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$addressables = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$firewall = Read-Json (Join-Path $evidenceRoot 'wave19-windows-firewall-contract.json')
$compilePath = Join-Path $evidenceRoot 'compile-result.txt'
$compileText = if (Test-Path -LiteralPath $compilePath -PathType Leaf) { Get-Content -LiteralPath $compilePath -Raw -Encoding UTF8 } else { '' }

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
foreach ($stage in @($stages | Select-Object -Skip 1)) {
    if ([int]$stage.exitCode -ne 0) { $infrastructureFailures.Add("$($stage.name) exited $($stage.exitCode)") }
}
if ([int]$searchStage.exitCode -ne 0) {
    $infrastructureFailures.Add("fresh GSN prerequisite exited $($searchStage.exitCode)")
}
if ($null -eq $gsnSummary -or [string]$gsnSummary.overall -ne 'GREEN' -or
    [string]$gsnSummary.productOverall -ne 'PASS' -or [string]$gsnSummary.infrastructureOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh GSN prerequisite is missing or not GREEN/PASS/PASS')
}
if ($null -eq $gsnSummary -or [int]$gsnSummary.searchNode.passed -ne 15 -or
    [int]$gsnSummary.searchNode.expectedGaps -ne 0 -or [int]$gsnSummary.searchNode.failed -ne 0) {
    $infrastructureFailures.Add('GSN GREEN lock is not exactly 15/15 PASS')
}
if ($null -eq $gsnSummary -or [int]$gsnSummary.wave19GreenLock.passed -ne 22 -or
    [int]$gsnSummary.wave19GreenLock.failed -ne 0) {
    $infrastructureFailures.Add('Wave 19 GREEN lock is not exactly 22/22 PASS')
}
if ($null -eq $gsnSummary -or [int]$gsnSummary.wave20GreenLock.passed -ne 16 -or
    [int]$gsnSummary.wave20GreenLock.failed -ne 0) {
    $infrastructureFailures.Add('Wave 20 GREEN lock is not exactly 16/16 PASS')
}
foreach ($report in @($waveBEdit, $waveBPlay)) {
    if ($null -eq $report -or [string]$report.infrastructureOverall -ne 'PASS' -or
        [string]$report.runId -ne $RunId -or [string]$report.baselineCommit -ne $BaselineCommit) {
        $infrastructureFailures.Add('a fresh Wave B report is missing, identity-mismatched, or infrastructure FAIL')
    }
}
if ($null -eq $waveBEditEvidence -or $null -eq $waveBPlayEvidence) {
    $infrastructureFailures.Add('structured Wave B Edit or Play observation evidence is missing')
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

$waveBChecks = @()
foreach ($report in @($waveBEdit, $waveBPlay)) { if ($null -ne $report) { $waveBChecks += @($report.checks) } }
$passes = @($waveBChecks | Where-Object { [string]$_.status -eq 'PASS' -and [string]$_.id -notlike 'GWB-I*' })
$expectedGaps = @($waveBChecks | Where-Object { [string]$_.status -eq 'EXPECTED_GAP' })
$productFailures = @($waveBChecks | Where-Object { [string]$_.status -eq 'FAIL' })
$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($productFailures.Count -gt 0) { 'FAIL' } elseif ($expectedGaps.Count -gt 0) { 'RED_EXPECTED_GAP' } else { 'PASS' }
$overall = if ($infrastructureOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'PASS') { 'GREEN' } else { 'RED' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$passIds = @($passes | ForEach-Object { [string]$_.id })
$gapIds = @($expectedGaps | ForEach-Object { [string]$_.id })
$failureIds = @($productFailures | ForEach-Object { [string]$_.id })
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-GameJamWaveBRedFirstGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"

$summary = [ordered]@{
    schemaVersion = 1
    title = 'GameJam Wave B RED-first independent QA gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    powershell = [ordered]@{ edition = $shellEdition; version = $shellVersion; executable = $shellExecutable }
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    prerequisites = [ordered]@{
        gsn = if ($null -eq $gsnSummary) { 'MISSING' } else { "$($gsnSummary.searchNode.passed)/15" }
        wave19 = if ($null -eq $gsnSummary) { 'MISSING' } else { "$($gsnSummary.wave19GreenLock.passed)/22" }
        wave20 = if ($null -eq $gsnSummary) { 'MISSING' } else { "$($gsnSummary.wave20GreenLock.passed)/16" }
    }
    waveB = [ordered]@{
        passed = $passes.Count
        expectedGaps = $expectedGaps.Count
        failed = $productFailures.Count
        passIds = $passIds
        expectedGapIds = $gapIds
        failureIds = $failureIds
    }
    currentLocks = [ordered]@{
        compile = if ($compileText -match 'Compiler errors:\s*0' -and $compileText -match 'Compiler warnings:\s*0') { 'PASS 0/0' } else { 'FAIL' }
        windowsDevelopmentBuild = if ($null -ne $windowsBuild -and [string]$windowsBuild.result -eq 'Succeeded') { 'PASS' } else { 'FAIL' }
        hiddenSmoke = if ($null -ne $smoke -and [string]$smoke.result -eq 'PASS') { "PASS $($smoke.observedSeconds)s" } else { 'FAIL' }
        addressables = if ($null -ne $addressables) { [string]$addressables.overall } else { 'MISSING' }
        firewallInboundBlock = if ($null -ne $firewall) { [string]$firewall.result } else { 'MISSING' }
        rawPlayerLog = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log')) { 'FAIL' } else { 'PASS_QUARANTINED' }
    }
    greenTransitionConditions = @(
        'exact public catalog observes 7 regions, 21 stable archetypes, 42 stable instances, and 144 derived general resource units',
        'same seed+contractRevision+lootTableRevision is byte-equal, another seed validly varies stock, and non-new-game transitions never regenerate stock',
        'hidden/partial/depleted, known remainder, broken barrier, and removed persistent hazard survive return, forced return, revisit, and snapshot restore',
        'one live natural disease trace completes telegraph, exposure, effect, worsen, and mitigate/treat with zero duplicate/cancel contamination',
        'ko/en/qps-long 1280x800 and keyboard/mouse/synthetic-gamepad meanings match without overflow, offscreen, player, or walking-band occlusion',
        'GSN 15/15, Wave 19 22/22, Wave 20 16/16, compile/build/smoke/Addressables/firewall remain PASS'
    )
    infrastructureFailures = @($infrastructureFailures | ForEach-Object { [string]$_ })
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    exactRerun = $exactRerun
    stages = @($stages | ForEach-Object { $_ })
    exitCode = $exitCode
}

$summaryPath = Join-Path $evidenceRoot 'gamejam-wave-b-summary.json'
$summaryTextPath = Join-Path $evidenceRoot 'gamejam-wave-b-summary.txt'
[IO.File]::WriteAllText($summaryPath, ($summary | ConvertTo-Json -Depth 20) + [Environment]::NewLine, $utf8NoBom)
$text = @(
    'GameJam Wave B RED-first independent QA gate'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "GSN/Wave19/Wave20 GREEN locks: $($summary.prerequisites.gsn)/$($summary.prerequisites.wave19)/$($summary.prerequisites.wave20)"
    "Wave B PASS/EXPECTED_GAP/FAIL: $($passes.Count)/$($expectedGaps.Count)/$($productFailures.Count)"
    "Expected gap IDs: $([string]::Join(', ', $gapIds))"
    "Product failure IDs: $([string]::Join(', ', $failureIds))"
    "Compile/build/smoke/Addressables/firewall: $($summary.currentLocks.compile)/$($summary.currentLocks.windowsDevelopmentBuild)/$($summary.currentLocks.hiddenSmoke)/$($summary.currentLocks.addressables)/$($summary.currentLocks.firewallInboundBlock)"
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
[IO.File]::WriteAllText((Join-Path $evidenceRoot 'gamejam-wave-b-powershell-compatibility.json'), ($compatibility | ConvertTo-Json -Depth 6) + [Environment]::NewLine, $utf8NoBom)
if ($compatibility.result -ne 'PASS') { exit 1 }

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "GSN_GREEN=$($summary.prerequisites.gsn)"
Write-Output "WAVE19_GREEN=$($summary.prerequisites.wave19)"
Write-Output "WAVE20_GREEN=$($summary.prerequisites.wave20)"
Write-Output "WAVE_B_PASS=$($passes.Count)"
Write-Output "WAVE_B_EXPECTED_GAP=$($expectedGaps.Count)"
Write-Output "WAVE_B_FAIL=$($productFailures.Count)"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output 'STEAM=NOT_READY'
Write-Output "EVIDENCE=$evidenceRoot"
exit $exitCode
