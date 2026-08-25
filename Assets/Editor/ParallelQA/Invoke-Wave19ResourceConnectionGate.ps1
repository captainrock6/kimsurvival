[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '2b985e1cbd08c82661bf71a4f82cbf6d63b4a97f',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(6, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'Wave 19 requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$wave17Entry = Join-Path $PSScriptRoot 'Invoke-Wave17PacingHazardGate.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $wave17Entry -PathType Leaf)) {
    throw "Wave 17 prerequisite entry point is missing: $wave17Entry"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 19 baseline mismatch. Expected $BaselineCommit, observed $head"
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

function Get-Sha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
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

# Wave 17 recursively runs Wave 16/15 and owns the fresh compile, Play loop,
# stable Windows Development build, hidden smoke, and Addressables evidence.
$wave17Arguments = @(
    '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $wave17Entry,
    '-RunId', $RunId, '-BaselineCommit', $BaselineCommit, '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', [string]$MinimumSmokeSeconds
)
$wave17Stage = Invoke-HiddenProcess 'fresh-wave17-green-prerequisite' $shellExecutable $wave17Arguments `
    (Join-Path $workRoot 'wave19-wave17-stdout.log') (Join-Path $workRoot 'wave19-wave17-stderr.log')

if (-not (Test-Path -LiteralPath $evidenceRoot)) {
    New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
}

$stages = New-Object System.Collections.Generic.List[object]
$stages.Add($wave17Stage)

foreach ($definition in @(
    [ordered]@{ name = 'wave18-edit-green-lock'; method = 'ParallelQA.Wave18GreenTransitionHardeningRunner.RunEditContracts'; play = $false },
    [ordered]@{ name = 'wave18-play-green-lock'; method = 'ParallelQA.Wave18GreenTransitionHardeningRunner.RunPlayContracts'; play = $true },
    [ordered]@{ name = 'wave19-edit-resource-contracts'; method = 'ParallelQA.Wave19ResourceConnectionGateRunner.RunEditContracts'; play = $false },
    [ordered]@{ name = 'wave19-play-resource-contracts'; method = 'ParallelQA.Wave19ResourceConnectionGateRunner.RunPlayContracts'; play = $true }
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

$wave17Summary = Read-Json (Join-Path $evidenceRoot 'wave17-summary.json')
$wave18Edit = Read-Json (Join-Path $evidenceRoot 'wave18-edit-contracts.json')
$wave18Play = Read-Json (Join-Path $evidenceRoot 'wave18-play-contracts.json')
$wave19Edit = Read-Json (Join-Path $evidenceRoot 'wave19-edit-contracts.json')
$wave19Play = Read-Json (Join-Path $evidenceRoot 'wave19-play-contracts.json')
$wave19Evidence = Read-Json (Join-Path $evidenceRoot 'wave19-play-observation-evidence.json')
$wave14 = Read-Json (Join-Path $evidenceRoot 'wave14-qps-global-layout-gate.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$smoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$addressables = Read-Json (Join-Path $evidenceRoot 'addressables-link-post-smoke-contract.json')
$compilePath = Join-Path $evidenceRoot 'compile-result.txt'
$compileText = if (Test-Path -LiteralPath $compilePath -PathType Leaf) { Get-Content -LiteralPath $compilePath -Raw -Encoding UTF8 } else { '' }

$expectedExecutable = [IO.Path]::GetFullPath((Join-Path $projectRoot 'work\ParallelQA\StableWindowsBuild\KimSurvivalIsland.exe'))
$currentExecutableSha256 = Get-Sha256 $expectedExecutable
$reportedBuildExecutable = if ($null -ne $windowsBuild -and -not [string]::IsNullOrWhiteSpace([string]$windowsBuild.executable)) { [IO.Path]::GetFullPath([string]$windowsBuild.executable) } else { '' }
$reportedSmokeExecutable = if ($null -ne $smoke -and -not [string]::IsNullOrWhiteSpace([string]$smoke.executable)) { [IO.Path]::GetFullPath([string]$smoke.executable) } else { '' }

# Do not reuse or overwrite the historical Artifacts/Verification firewall
# report. Query Windows for this exact worktree's stable executable instead.
$matchingFirewallRules = New-Object System.Collections.Generic.List[object]
try {
    $candidateRules = @(Get-NetFirewallRule -ErrorAction Stop | Where-Object {
        [string]$_.Direction -eq 'Inbound' -and [string]$_.Action -eq 'Block'
    })
    foreach ($rule in $candidateRules) {
        $filter = $rule | Get-NetFirewallApplicationFilter -ErrorAction SilentlyContinue
        if ($null -eq $filter -or [string]::IsNullOrWhiteSpace([string]$filter.Program)) { continue }
        $observedProgram = [IO.Path]::GetFullPath([string]$filter.Program)
        if ($observedProgram.Equals($expectedExecutable, [StringComparison]::OrdinalIgnoreCase)) {
            $matchingFirewallRules.Add([ordered]@{
                name = [string]$rule.Name
                displayName = [string]$rule.DisplayName
                enabled = [string]$rule.Enabled
                direction = [string]$rule.Direction
                action = [string]$rule.Action
                program = $observedProgram
            })
        }
    }
} catch {
    $matchingFirewallRules.Add([ordered]@{ queryError = $_.Exception.Message })
}
$verifiedFirewallRules = @($matchingFirewallRules | Where-Object {
    [string]$_.enabled -eq 'True' -and [string]$_.direction -eq 'Inbound' -and
    [string]$_.action -eq 'Block' -and [string]$_.program -eq $expectedExecutable
})
$firewallPass = $verifiedFirewallRules.Count -ge 1
$matchingFirewallRuleSnapshot = @($matchingFirewallRules | ForEach-Object { $_ })

$buildIdentityPass = $null -ne $windowsBuild -and [string]$windowsBuild.runId -eq $RunId -and
    [string]$windowsBuild.baselineCommit -eq $BaselineCommit -and [string]$windowsBuild.result -eq 'Succeeded' -and
    [string]$windowsBuild.options -eq 'Development' -and [int]$windowsBuild.errors -eq 0 -and [int]$windowsBuild.warnings -eq 0 -and
    $reportedBuildExecutable.Equals($expectedExecutable, [StringComparison]::OrdinalIgnoreCase) -and
    -not [string]::IsNullOrWhiteSpace($currentExecutableSha256) -and [string]$windowsBuild.executableSha256 -eq $currentExecutableSha256
$smokeIdentityPass = $null -ne $smoke -and [string]$smoke.runId -eq $RunId -and
    [string]$smoke.baselineCommit -eq $BaselineCommit -and [string]$smoke.result -eq 'PASS' -and
    [double]$smoke.observedSeconds -ge $MinimumSmokeSeconds -and [bool]$smoke.aliveAtMinimum -and [bool]$smoke.respondingAtMinimum -and
    $reportedSmokeExecutable.Equals($expectedExecutable, [StringComparison]::OrdinalIgnoreCase) -and
    [string]$smoke.executableSha256 -eq $currentExecutableSha256 -and [bool]$smoke.stableExecutablePath -and [bool]$smoke.buildIdentityMatches

$firewallContract = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $BaselineCommit
    expectedStableExecutable = $expectedExecutable
    executableSha256 = $currentExecutableSha256
    buildIdentity = if ($buildIdentityPass) { 'PASS' } else { 'FAIL' }
    smokeIdentity = if ($smokeIdentityPass) { 'PASS' } else { 'FAIL' }
    # Windows PowerShell 5.1 cannot reliably expand a generic List[object]
    # directly inside an ordered hashtable ("Argument types do not match").
    # Materialize it through the pipeline so the JSON shape stays unchanged on
    # both Desktop 5.1 and Core 7.x.
    matchingRules = $matchingFirewallRuleSnapshot
    result = if ($buildIdentityPass -and $smokeIdentityPass -and $firewallPass) { 'PASS' } else { 'FAIL' }
}
$firewallContractPath = Join-Path $evidenceRoot 'wave19-windows-firewall-contract.json'
[IO.File]::WriteAllText($firewallContractPath, ($firewallContract | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)

$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ([int]$wave17Stage.exitCode -ne 0) { $infrastructureFailures.Add("Wave 17 prerequisite exited $($wave17Stage.exitCode)") }
foreach ($stage in @($stages | Select-Object -Skip 1)) {
    if ([int]$stage.exitCode -ne 0) { $infrastructureFailures.Add("$($stage.name) exited $($stage.exitCode)") }
}
if ($null -eq $wave17Summary -or [string]$wave17Summary.infrastructureOverall -ne 'PASS' -or [string]$wave17Summary.productOverall -ne 'PASS') {
    $infrastructureFailures.Add('fresh Wave 17 prerequisite is missing or not GREEN/PASS')
}
foreach ($report in @($wave18Edit, $wave18Play, $wave19Edit, $wave19Play)) {
    if ($null -eq $report -or [string]$report.infrastructureOverall -ne 'PASS') {
        $infrastructureFailures.Add('a fresh Wave 18/19 machine-readable report is missing or infrastructure FAIL')
    }
}
$wave18Checks = @()
foreach ($report in @($wave18Edit, $wave18Play)) { if ($null -ne $report) { $wave18Checks += @($report.checks) } }
$wave18ProductChecks = @($wave18Checks | Where-Object {
    [string]$_.id -like 'W17-*' -and [string]$_.id -notin @('W17-HW01.physical_gamepad', 'W17-S01.steam_release')
})
$wave18ManualChecks = @($wave18Checks | Where-Object {
    [string]$_.id -in @('W17-HW01.physical_gamepad', 'W17-S01.steam_release')
})
if ($wave18ProductChecks.Count -ne 23 -or @($wave18ProductChecks | Where-Object { [string]$_.status -ne 'PASS' }).Count -ne 0) {
    $infrastructureFailures.Add("current Wave 18 GREEN matrix is not exactly 23/23 PASS; observed=$($wave18ProductChecks.Count)")
}
if ($wave18ManualChecks.Count -ne 2 -or @($wave18ManualChecks | Where-Object { [string]$_.status -ne 'UNVERIFIED' }).Count -ne 0) {
    $infrastructureFailures.Add('Wave 18 physical-gamepad and Steam manual gates were not preserved as two UNVERIFIED checks')
}
if ($compileText -notmatch 'Compiler errors:\s*0' -or $compileText -notmatch 'Compiler warnings:\s*0') {
    $infrastructureFailures.Add('Unity compile is not 0 errors / 0 warnings')
}
if ($null -eq $wave14 -or [string]$wave14.productOverall -ne 'PASS' -or [string]$wave14.infrastructureOverall -ne 'PASS' -or
    [int]$wave14.targetCount -ne 10 -or [int]$wave14.passedTargets -ne 10) {
    $infrastructureFailures.Add('qps-long global layout lock is not 10/10 PASS')
}
if (-not $buildIdentityPass) { $infrastructureFailures.Add('Windows StableWindowsBuild RunId/baseline/path/SHA/development/warnings identity failed') }
if (-not $smokeIdentityPass) { $infrastructureFailures.Add("hidden Windows smoke did not remain alive/responding for at least $MinimumSmokeSeconds seconds on the exact build SHA") }
if (-not $firewallPass) { $infrastructureFailures.Add('the exact current worktree StableWindowsBuild executable has no enabled Inbound Block rule') }
if ($null -eq $addressables -or [string]$addressables.overall -ne 'PASS') { $infrastructureFailures.Add('Addressables post-smoke contract is not PASS') }
if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log') -PathType Leaf) { $infrastructureFailures.Add('raw Windows Player log escaped quarantine') }
if ($null -eq $wave19Evidence -or @($wave19Evidence.layouts).Count -ne 12) { $infrastructureFailures.Add('Wave 19 did not emit exactly 12 locale/surface layout observations') }

$wave19Checks = @()
foreach ($report in @($wave19Edit, $wave19Play)) { if ($null -ne $report) { $wave19Checks += @($report.checks) } }
$productFailures = @($wave19Checks | Where-Object { [string]$_.status -eq 'FAIL' })
$productPasses = @($wave19Checks | Where-Object { [string]$_.status -eq 'PASS' })
$unverified = @($wave19Checks | Where-Object { [string]$_.status -eq 'UNVERIFIED' })
$notReady = @($wave19Checks | Where-Object { [string]$_.status -eq 'NOT_READY' })
$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$productOverall = if ($productFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$overall = if ($infrastructureOverall -eq 'FAIL') { 'FAIL' } elseif ($productOverall -eq 'PASS') { 'GREEN' } else { 'RED' }
$exitCode = if ($overall -eq 'GREEN') { 0 } elseif ($overall -eq 'RED') { 2 } else { 1 }
$exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave19ResourceConnectionGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit' -MinimumSmokeSeconds $MinimumSmokeSeconds"
$infrastructureFailureSnapshot = @($infrastructureFailures | ForEach-Object { [string]$_ })
$stageSnapshot = @($stages | ForEach-Object { $_ })

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Wave 19 independent resource connection visual regression gate'
    runId = $RunId
    baselineCommit = $BaselineCommit
    startedUtc = $runStarted.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    powershell = [ordered]@{ edition = $shellEdition; version = $shellVersion; executable = $shellExecutable }
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    wave18GreenLock = [ordered]@{ total = 23; passed = @($wave18ProductChecks | Where-Object status -eq 'PASS').Count; failed = @($wave18ProductChecks | Where-Object status -ne 'PASS').Count }
    wave19 = [ordered]@{
        passed = $productPasses.Count
        failed = $productFailures.Count
        failureIds = @($productFailures | ForEach-Object { [string]$_.id })
        unverified = $unverified.Count
        notReady = $notReady.Count
        layoutObservations = if ($null -eq $wave19Evidence) { 0 } else { @($wave19Evidence.layouts).Count }
    }
    greenLocks = [ordered]@{
        compile = if ($compileText -match 'Compiler errors:\s*0' -and $compileText -match 'Compiler warnings:\s*0') { 'PASS 0/0' } else { 'FAIL' }
        qpsLong = if ($null -ne $wave14) { "$($wave14.passedTargets)/$($wave14.targetCount)" } else { 'MISSING' }
        windowsDevelopmentBuild = if ($buildIdentityPass) { 'PASS' } else { 'FAIL' }
        hiddenSmoke = if ($smokeIdentityPass) { "PASS $($smoke.observedSeconds)s" } else { 'FAIL' }
        addressables = if ($null -ne $addressables) { [string]$addressables.overall } else { 'MISSING' }
        firewallInboundBlock = if ($firewallPass) { 'PASS' } else { 'FAIL' }
        rawPlayerLog = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log')) { 'FAIL' } else { 'PASS_QUARANTINED' }
    }
    stableWindowsBuild = [ordered]@{
        executable = $expectedExecutable
        sha256 = $currentExecutableSha256
    }
    infrastructureFailures = $infrastructureFailureSnapshot
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    exactRerun = $exactRerun
    stages = $stageSnapshot
    exitCode = $exitCode
}

$summaryPath = Join-Path $evidenceRoot 'wave19-summary.json'
$summaryTextPath = Join-Path $evidenceRoot 'wave19-summary.txt'
[IO.File]::WriteAllText($summaryPath, ($summary | ConvertTo-Json -Depth 20) + [Environment]::NewLine, $utf8NoBom)
$text = @(
    'Wave 19 independent resource connection visual regression gate'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall"
    "Wave 18 GREEN lock: $($summary.wave18GreenLock.passed)/$($summary.wave18GreenLock.total)"
    "Wave 19 PASS/FAIL: $($productPasses.Count)/$($productFailures.Count)"
    "Failure IDs: $([string]::Join(', ', @($summary.wave19.failureIds)))"
    "Compile/qps/build/smoke/Addressables/firewall: $($summary.greenLocks.compile)/$($summary.greenLocks.qpsLong)/$($summary.greenLocks.windowsDevelopmentBuild)/$($summary.greenLocks.hiddenSmoke)/$($summary.greenLocks.addressables)/$($summary.greenLocks.firewallInboundBlock)"
    "Stable executable: $expectedExecutable"
    "Stable executable SHA-256: $currentExecutableSha256"
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
    firewallUtf8NoBom = Test-Utf8NoBom $firewallContractPath
    rawPlayerLogQuarantined = -not (Test-Path -LiteralPath (Join-Path $evidenceRoot 'windows-player.log'))
}
$compatibility.result = if ($compatibility.summaryUtf8NoBom -and $compatibility.firewallUtf8NoBom -and $compatibility.rawPlayerLogQuarantined) { 'PASS' } else { 'FAIL' }
[IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave19-powershell-compatibility.json'), ($compatibility | ConvertTo-Json -Depth 6) + [Environment]::NewLine, $utf8NoBom)
if ($compatibility.result -ne 'PASS') { exit 1 }

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "WAVE18_GREEN=$($summary.wave18GreenLock.passed)/23"
Write-Output "WAVE19_PASS=$($productPasses.Count)"
Write-Output "WAVE19_FAIL=$($productFailures.Count)"
Write-Output "PHYSICAL_GAMEPAD=UNVERIFIED"
Write-Output "STEAM=NOT_READY"
Write-Output "EVIDENCE=$evidenceRoot"
exit $exitCode
