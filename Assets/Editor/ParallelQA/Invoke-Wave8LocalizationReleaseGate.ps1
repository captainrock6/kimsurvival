[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '2a542be0c2c9fa0a49f501bab1965bb59b5f06f3',

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe',

    [ValidateRange(5, 60)]
    [int]$MinimumSmokeSeconds = 6
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$wave7Script = Join-Path $PSScriptRoot 'Invoke-Wave7BagCapacityRegression.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (-not (Test-Path -LiteralPath $wave7Script -PathType Leaf)) {
    throw "Wave 7 regression script not found: $wave7Script"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a new run ID: $evidenceRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 8 baseline mismatch. Expected $BaselineCommit, observed $head"
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
    [string]$LogFile
) {
    $arguments = @(
        '-batchmode',
        '-nographics',
        '-quit',
        '-projectPath', $projectRoot,
        '-executeMethod', $ExecuteMethod,
        '-logFile', $LogFile
    )
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
    [object]$Check
) {
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

function Read-InvariantDouble([string]$Value) {
    return [double]::Parse($Value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Measure-QpsWorldBlockOverlaps([string]$MetricsPath) {
    $result = [ordered]@{
        schemaVersion = 1
        runId = $RunId
        baselineCommit = $BaselineCommit
        sourceMetrics = $MetricsPath
        scenario = 'qps-long placement valid'
        scope = 'synthetic QA text mutation; not an actual qps-long locale'
        minimumSignificantBlockOverlap = 0.05
        legacyGlyphGate = 'reported separately; this independent block gate does not replace existing pixel thresholds'
        overall = 'MISSING'
        overlaps = @()
    }
    if (-not (Test-Path -LiteralPath $MetricsPath -PathType Leaf)) { return $result }

    $world = @(Import-Csv -LiteralPath $MetricsPath -Delimiter "`t" | Where-Object {
        $_.scenario -eq 'qps-long placement valid' -and
        $_.category -eq 'pseudo-long' -and
        $_.hierarchy -match 'Runtime Placeholder World'
    })
    $overlaps = New-Object System.Collections.Generic.List[object]
    for ($leftIndex = 0; $leftIndex -lt $world.Count; $leftIndex += 1) {
        for ($rightIndex = $leftIndex + 1; $rightIndex -lt $world.Count; $rightIndex += 1) {
            $left = $world[$leftIndex]
            $right = $world[$rightIndex]
            $intersectionWidth = [Math]::Max(0, [Math]::Min((Read-InvariantDouble $left.right_px), (Read-InvariantDouble $right.right_px)) - [Math]::Max((Read-InvariantDouble $left.left_px), (Read-InvariantDouble $right.left_px)))
            $intersectionHeight = [Math]::Max(0, [Math]::Min((Read-InvariantDouble $left.top_px), (Read-InvariantDouble $right.top_px)) - [Math]::Max((Read-InvariantDouble $left.bottom_px), (Read-InvariantDouble $right.bottom_px)))
            $leftArea = [Math]::Max(1, ((Read-InvariantDouble $left.right_px) - (Read-InvariantDouble $left.left_px)) * ((Read-InvariantDouble $left.top_px) - (Read-InvariantDouble $left.bottom_px)))
            $rightArea = [Math]::Max(1, ((Read-InvariantDouble $right.right_px) - (Read-InvariantDouble $right.left_px)) * ((Read-InvariantDouble $right.top_px) - (Read-InvariantDouble $right.bottom_px)))
            $ratio = ($intersectionWidth * $intersectionHeight) / [Math]::Min($leftArea, $rightArea)
            if ($ratio -ge $result.minimumSignificantBlockOverlap) {
                $overlaps.Add([ordered]@{
                    leftHierarchy = $left.hierarchy
                    leftText = $left.text
                    rightHierarchy = $right.hierarchy
                    rightText = $right.text
                    intersectionWidthPixels = [Math]::Round($intersectionWidth, 1)
                    intersectionHeightPixels = [Math]::Round($intersectionHeight, 1)
                    overlapOfSmallerBlock = [Math]::Round($ratio, 4)
                })
            }
        }
    }
    $result.overlaps = $overlaps.ToArray()
    $result.overall = if ($overlaps.Count -eq 0) { 'PASS' } else { 'FAIL' }
    return $result
}

$stages = New-Object System.Collections.Generic.List[object]
$wave7Started = [DateTime]::UtcNow
$hostExecutable = (Get-Process -Id $PID).Path
$wave7Stdout = Join-Path $workRoot 'wave7-child-stdout.log'
$wave7Stderr = Join-Path $workRoot 'wave7-child-stderr.log'
$wave7Arguments = @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $wave7Script,
    '-RunId', $RunId,
    '-BaselineCommit', $BaselineCommit,
    '-UnityPath', $UnityPath,
    '-MinimumSmokeSeconds', $MinimumSmokeSeconds
)
$wave7ArgumentLine = [string]::Join(' ', @($wave7Arguments | ForEach-Object { Quote-Argument $_ }))
$wave7Process = Start-Process -FilePath $hostExecutable -ArgumentList $wave7ArgumentLine -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $wave7Stdout -RedirectStandardError $wave7Stderr
$stages.Add([ordered]@{
    name = 'fresh-wave7-full-regression'
    executeMethod = 'Invoke-Wave7BagCapacityRegression.ps1'
    startedUtc = $wave7Started.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    exitCode = $wave7Process.ExitCode
    log = $wave7Stdout
    stderr = $wave7Stderr
    command = (Quote-Argument $hostExecutable) + ' ' + $wave7ArgumentLine
})

if (-not (Test-Path -LiteralPath $evidenceRoot -PathType Container)) {
    throw "Fresh Wave 7 regression did not create evidence: $evidenceRoot"
}

$stages.Add((Invoke-UnityStage 'wave8-localization-edit-contracts' 'ParallelQA.Wave8LocalizationReleaseGateRunner.RunEditContracts' (Join-Path $workRoot 'unity-wave8-localization-edit.log')))
$stages.Add((Invoke-UnityStage 'locale-relaunch-prepare' 'ParallelQA.ParallelQaRunner.PrepareLocalePersistenceProbe' (Join-Path $workRoot 'unity-wave8-locale-prepare.log')))
$stages.Add((Invoke-UnityStage 'locale-relaunch-verify' 'ParallelQA.ParallelQaRunner.VerifyLocalePersistenceProbe' (Join-Path $workRoot 'unity-wave8-locale-verify.log')))

$commandResults = [ordered]@{
    schemaVersion = 1
    title = '국제화 릴리스 게이트 독립 검증'
    runId = $RunId
    baselineCommit = $BaselineCommit
    unityVersionExpected = '6000.4.9f1'
    unityExecutable = $UnityPath
    invokedUtc = [DateTime]::UtcNow.ToString('O')
    executionPolicy = 'All Unity Editor/build and Windows Player processes ran outside the Codex sandbox; no -noUpm.'
    integratedEvidencePolicy = 'Prior Wave 7 evidence was audited for test design only; every verdict below comes from this new run ID.'
    exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-Wave8LocalizationReleaseGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '$BaselineCommit'"
    stages = $stages.ToArray()
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave8-command-results.json'), ($commandResults | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)

$wave7Summary = Read-Json (Join-Path $evidenceRoot 'wave7-summary.json')
$wave8Edit = Read-Json (Join-Path $evidenceRoot 'wave8-edit-contracts.json')
$visual = Read-Json (Join-Path $evidenceRoot 'wave5-current-visual-facts.json')
$wave7Layout = Read-Json (Join-Path $evidenceRoot 'wave7-layout-metrics.json')
$windowsBuild = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$windowsSmoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
$steam = Read-Json (Join-Path $evidenceRoot 'steam-readiness.json')
$persistenceText = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'locale-relaunch-persistence.txt')) {
    Get-Content -LiteralPath (Join-Path $evidenceRoot 'locale-relaunch-persistence.txt') -Raw
} else { '' }
$compileText = if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt')) {
    Get-Content -LiteralPath (Join-Path $evidenceRoot 'compile-result.txt') -Raw
} else { '' }

$stageByName = @{}
foreach ($stage in $stages) { $stageByName[$stage.name] = $stage }
$infrastructureFailures = New-Object System.Collections.Generic.List[string]
if ($null -eq $wave7Summary -or $wave7Summary.infrastructureOverall -ne 'PASS' -or $stageByName['fresh-wave7-full-regression'].exitCode -notin @(0, 2)) {
    $infrastructureFailures.Add('fresh Wave 7 full regression did not complete with parseable infrastructure PASS evidence')
}
if ($null -eq $wave8Edit -or $wave8Edit.infrastructureOverall -ne 'PASS' -or $stageByName['wave8-localization-edit-contracts'].exitCode -notin @(0, 1)) {
    $infrastructureFailures.Add('Wave 8 localization Edit contract did not complete with parseable infrastructure evidence')
}
if ($stageByName['locale-relaunch-prepare'].exitCode -ne 0 -or $stageByName['locale-relaunch-verify'].exitCode -ne 0 -or $persistenceText -notmatch 'PASS\s+·\s+A fresh Unity process restored') {
    $infrastructureFailures.Add('separate-process locale persistence probe did not pass')
}
if ($compileText -notmatch 'Result:\s+PASS' -or $compileText -notmatch 'Compiler errors:\s+0') {
    $infrastructureFailures.Add('fresh compile did not prove PASS with zero compiler errors')
}

$productDefects = New-Object System.Collections.Generic.List[object]
if ($null -ne $wave8Edit) {
    foreach ($check in @($wave8Edit.checks | Where-Object { $_.status -in @('FAIL', 'NOT_IMPLEMENTED') })) {
        Add-Defect $productDefects $check
    }
}
if ($null -eq $wave7Summary -or $wave7Summary.productOverall -ne 'PASS') {
    $productDefects.Add([ordered]@{
        id = 'W8-R01.wave7_full_regression'
        severity = 'P0'
        classification = 'PRODUCT_REGRESSION'
        expected = 'Fresh Wave 7 full regression product PASS'
        actual = if ($null -eq $wave7Summary) { 'Wave 7 summary missing' } else { 'Wave 7 product=' + $wave7Summary.productOverall }
        reproduction = "Run Invoke-Wave7BagCapacityRegression.ps1 with run ID $RunId."
        recommendedFiles = 'inspect the fresh Wave 7 defect inventory before assigning product ownership'
    })
}

$placementPass = $null -ne $visual -and $visual.placement.status -eq 'PASS' -and $visual.placement.targets -eq 24 -and $visual.placement.failures -eq 0
$explorationPass = $null -ne $visual -and $visual.explorationSwimming.status -eq 'PASS' -and $visual.explorationSwimming.targets -eq 10 -and $visual.explorationSwimming.failures -eq 0
$syntheticQpsPass = $null -ne $visual -and $visual.qpsLong.status -eq 'PASS' -and $visual.qpsLong.targets -eq 10 -and $visual.qpsLong.failures -eq 0
$layoutPass = $null -ne $wave7Layout -and $wave7Layout.overall -eq 'PASS'
$qpsBlockAudit = Measure-QpsWorldBlockOverlaps (Join-Path $evidenceRoot 'wave3-visual-metrics.tsv')
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave8-qps-world-block-overlap.json'), ($qpsBlockAudit | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)
$qpsBlockLines = New-Object System.Collections.Generic.List[string]
$qpsBlockLines.Add('Wave 8 independent qps stress world-label block overlap gate')
$qpsBlockLines.Add("Run ID: $RunId")
$qpsBlockLines.Add("Baseline: $BaselineCommit")
$qpsBlockLines.Add("Scope: $($qpsBlockAudit.scope)")
$qpsBlockLines.Add("Threshold: overlap of smaller projected text block < $([double]$qpsBlockAudit.minimumSignificantBlockOverlap * 100)%")
$qpsBlockLines.Add("Overall: $($qpsBlockAudit.overall)")
foreach ($overlap in @($qpsBlockAudit.overlaps)) {
    $qpsBlockLines.Add("FAIL · $($overlap.leftText) <> $($overlap.rightText) · intersection=$($overlap.intersectionWidthPixels)x$($overlap.intersectionHeightPixels)px · smallerBlock=$([double]$overlap.overlapOfSmallerBlock * 100)%")
}
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave8-qps-world-block-overlap.txt'), $qpsBlockLines.ToArray(), $utf8NoBom)
if ($qpsBlockAudit.overall -eq 'FAIL') {
    $productDefects.Add([ordered]@{
        id = 'W8-U01.qps_stress_world_label_overlap'
        severity = 'P2'
        classification = 'PRODUCT_LAYOUT_DEFECT'
        expected = 'At 1280x800, qps stress world-label text blocks remain separated with less than 5% overlap of the smaller block.'
        actual = "Independent block gate found $(@($qpsBlockAudit.overlaps).Count) significant overlaps; the legacy 15% gate still reports 10/10 PASS."
        reproduction = 'Open playmode-qps-long-placement-1280x800.png at 1:1 and compare projected blocks in wave3-visual-metrics.tsv.'
        recommendedFiles = 'Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/Editor/ParallelQA/Wave3VisualGate.cs'
    })
}
$qpsDataCheck = if ($null -ne $wave8Edit) { @($wave8Edit.checks | Where-Object id -eq 'W8-E05.qps_long_data_registration' | Select-Object -First 1) } else { @() }
$qpsExpansionCheck = if ($null -ne $wave8Edit) { @($wave8Edit.checks | Where-Object id -eq 'W8-E06.qps_long_expansion_tokens_glyphs' | Select-Object -First 1) } else { @() }
$actualQpsReady = $qpsDataCheck.Count -eq 1 -and $qpsExpansionCheck.Count -eq 1 -and $qpsDataCheck[0].status -eq 'PASS' -and $qpsExpansionCheck[0].status -eq 'PASS'
$windowsBuildPass = $null -ne $windowsBuild -and $windowsBuild.result -eq 'Succeeded' -and $windowsBuild.errors -eq 0 -and $windowsBuild.executableExists
$windowsSmokePass = $null -ne $windowsSmoke -and $windowsSmoke.result -eq 'PASS' -and $windowsSmoke.aliveAtMinimum -and $windowsSmoke.respondingAtMinimum
$warnings = if ($null -ne $windowsBuild) { [int]$windowsBuild.warnings } else { -1 }
$steamReadiness = if ($null -ne $steam -and -not [string]::IsNullOrWhiteSpace([string]$steam.overall)) { [string]$steam.overall } else { 'NOT_READY' }

$productOverall = if ($productDefects.Count -eq 0 -and $actualQpsReady) { 'PASS' } else { 'FAIL' }
$infrastructureOverall = if ($infrastructureFailures.Count -eq 0) { 'PASS' } else { 'FAIL' }
$overall = if ($productOverall -eq 'PASS' -and $infrastructureOverall -eq 'PASS') { 'PASS' } else { 'FAIL' }

$summary = [ordered]@{
    schemaVersion = 1
    title = '국제화 릴리스 게이트 독립 검증'
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    overall = $overall
    productOverall = $productOverall
    infrastructureOverall = $infrastructureOverall
    compile = if ($compileText -match 'Result:\s+PASS' -and $compileText -match 'Compiler errors:\s+0') { 'PASS 0 errors' } else { 'FAIL' }
    wave8Edit = if ($null -ne $wave8Edit) { $wave8Edit.productOverall } else { 'MISSING' }
    localeRelaunchEditorProcesses = if ($persistenceText -match 'PASS\s+·\s+A fresh Unity process restored') { 'PASS' } else { 'FAIL' }
    wave7FullRegression = if ($null -ne $wave7Summary -and $wave7Summary.productOverall -eq 'PASS' -and $wave7Summary.infrastructureOverall -eq 'PASS') { 'PASS' } else { 'FAIL' }
    placement = if ($placementPass) { 'PASS 24/24' } else { 'FAIL' }
    explorationSwimming = if ($explorationPass) { 'PASS 10/10' } else { 'FAIL' }
    koEnBagLayout1280And1920 = if ($layoutPass) { 'PASS' } else { 'FAIL' }
    syntheticPseudoVisualFixture = if ($syntheticQpsPass) { 'LEGACY PASS 10/10 · QA text mutation only' } else { 'LEGACY FAIL' }
    independentQpsStressBlockOverlap = [string]$qpsBlockAudit.overall
    actualQpsLongLocale = if ($actualQpsReady) { 'PASS' } else { 'NOT_IMPLEMENTED' }
    actualQpsLayout1280And1920 = if ($actualQpsReady -and $syntheticQpsPass -and $layoutPass) { 'PASS' } else { 'NOT_IMPLEMENTED · synthetic fixture cannot substitute for an actual locale' }
    windowsDevelopmentBuild = if ($windowsBuildPass) { 'PASS' } else { 'FAIL' }
    windowsBuildWarnings = $warnings
    hiddenSmoke = if ($windowsSmokePass) { 'PASS' } else { 'FAIL' }
    physicalGamepad = 'UNVERIFIED'
    physicalGamepadReason = 'No physical-device human actuation was performed; synthetic/code-path automation cannot upgrade this status.'
    windowsPlayerLocaleRelaunch = 'UNVERIFIED · Editor-process persistence is automated; no in-player locale selection/normal-exit/relaunch actuation was captured.'
    steamReadiness = $steamReadiness
    steamReadyClaim = $false
    productDefects = $productDefects.ToArray()
    infrastructureFailures = $infrastructureFailures.ToArray()
    manualGates = @(
        'Physical gamepad locale settings and ko/en core-loop human actuation',
        'Windows Player locale selection, normal exit, and relaunch-first-frame persistence',
        'Human 1:1 review of actual qps-long camp/HUD/placement/swim/bag/result frames after the locale exists',
        'Native-language review and official English game title decision'
    )
    evidenceRoot = $evidenceRoot
    exactRerun = $commandResults.exactRerun
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave8-summary.json'), ($summary | ConvertTo-Json -Depth 14) + [Environment]::NewLine, $utf8NoBom)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('Wave 8 independent localization release gate summary')
$lines.Add("Run ID: $RunId")
$lines.Add("Baseline: $BaselineCommit")
$lines.Add("Overall: $overall")
$lines.Add("Product/Infrastructure: $productOverall/$infrastructureOverall")
$lines.Add("Compile: $($summary.compile)")
$lines.Add("Wave 8 Edit: $($summary.wave8Edit)")
$lines.Add("Editor separate-process locale persistence: $($summary.localeRelaunchEditorProcesses)")
$lines.Add("Fresh Wave 7 full regression: $($summary.wave7FullRegression)")
$lines.Add("Placement / exploration-swimming: $($summary.placement) / $($summary.explorationSwimming)")
$lines.Add("KO/EN 1280x800+1920x1080 bag layout: $($summary.koEnBagLayout1280And1920)")
$lines.Add("Synthetic pseudo visual fixture: $($summary.syntheticPseudoVisualFixture)")
$lines.Add("Independent qps stress world-label block overlap: $($summary.independentQpsStressBlockOverlap)")
$lines.Add("Actual qps-long locale/layout: $($summary.actualQpsLongLocale) / $($summary.actualQpsLayout1280And1920)")
$lines.Add("Windows Development build/smoke: $($summary.windowsDevelopmentBuild)/$($summary.hiddenSmoke) · warnings=$warnings")
$lines.Add("Physical gamepad: $($summary.physicalGamepad)")
$lines.Add("Windows Player locale relaunch: $($summary.windowsPlayerLocaleRelaunch)")
$lines.Add("Steam: $steamReadiness · READY claim=false")
$lines.Add("Product finding count: $($productDefects.Count)")
$lines.Add("Infrastructure failure count: $($infrastructureFailures.Count)")
$lines.Add("Exact rerun: $($summary.exactRerun)")
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave8-summary.txt'), $lines.ToArray(), $utf8NoBom)

$metadata = New-Object System.Collections.Generic.List[string]
$metadata.Add('Wave 8 run metadata')
$metadata.Add("Title: 국제화 릴리스 게이트 독립 검증")
$metadata.Add("Run ID: $RunId")
$metadata.Add("Baseline: $BaselineCommit")
$metadata.Add("Branch: $((& git -C $projectRoot branch --show-current).Trim())")
$metadata.Add('Unity: 6000.4.9f1')
$metadata.Add("Started/completed UTC: $($wave7Started.ToString('O')) / $([DateTime]::UtcNow.ToString('O'))")
$metadata.Add("OS: $([System.Environment]::OSVersion.VersionString)")
$metadata.Add('Execution: sandbox_permissions=require_escalated; no -noUpm')
$metadata.Add('Display contracts: fresh 1280x800 full-loop/visual frames plus 1280x800 and 1920x1080 Wave 7 bag frames')
$metadata.Add('Physical gamepad: UNVERIFIED')
$metadata.Add("Overall/Product/Infrastructure: $overall/$productOverall/$infrastructureOverall")
$metadata.Add("Exact rerun: $($summary.exactRerun)")
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'run-metadata.txt'), $metadata.ToArray(), $utf8NoBom)

Write-Output "OVERALL=$overall"
Write-Output "PRODUCT=$productOverall"
Write-Output "INFRASTRUCTURE=$infrastructureOverall"
Write-Output "WAVE7_FULL=$($summary.wave7FullRegression)"
Write-Output "ACTUAL_QPS_LONG=$($summary.actualQpsLongLocale)"
Write-Output "PHYSICAL_GAMEPAD=$($summary.physicalGamepad)"
Write-Output "STEAM=$steamReadiness"
Write-Output "EVIDENCE=$evidenceRoot"

if ($infrastructureFailures.Count -gt 0) { exit 3 }
if ($productOverall -ne 'PASS') { exit 2 }
exit 0
