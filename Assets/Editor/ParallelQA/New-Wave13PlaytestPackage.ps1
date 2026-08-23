[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$BaselineCommit = '386a602f110ebfe2c404685f98f9cacf1b42c1d2'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$buildRoot = Join-Path $workRoot 'WindowsBuild'
$runToken = if ($RunId.Length -le 24) { $RunId } else { $RunId.Substring(0, 12) + '_' + $RunId.Substring($RunId.Length - 11) }
$kitRoot = Join-Path $projectRoot (Join-Path 'work\W13' $runToken)
$packageRoot = Join-Path $kitRoot 'payload'
$zipPath = Join-Path $kitRoot 'KimSurvivalIsland-Windows-x64-Development.zip'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Get-Sha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-RelativePath([string]$BasePath, [string]$Path) {
    $baseWithSlash = $BasePath.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $baseUri = New-Object System.Uri($baseWithSlash)
    $pathUri = New-Object System.Uri($Path)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace('\', '/')
}

function Get-StringSha256([string]$Value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $utf8NoBom.GetBytes($Value)
        return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
}

function Get-PayloadFiles([string]$Root) {
    return @(Get-ChildItem -LiteralPath $Root -Recurse -File | Sort-Object FullName | ForEach-Object {
        [pscustomobject][ordered]@{
            path = Get-RelativePath $Root $_.FullName
            bytes = $_.Length
            sha256 = Get-Sha256 $_.FullName
        }
    })
}

function Get-DirectoryContract([string]$Name, [string]$Path, [string]$RelativePath, $PayloadFiles) {
    $prefix = $RelativePath.TrimEnd('/') + '/'
    $files = @($PayloadFiles | Where-Object { $_.path.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase) })
    $digestSource = [string]::Join("`n", @($files | ForEach-Object { "$($_.sha256)  $($_.path)" }))
    return [ordered]@{
        name = $Name
        kind = 'directory'
        path = $RelativePath
        exists = Test-Path -LiteralPath $Path -PathType Container
        fileCount = $files.Count
        bytes = [long](($files | Measure-Object -Property bytes -Sum).Sum)
        aggregateSha256 = if ($files.Count -gt 0) { Get-StringSha256 $digestSource } else { '' }
        status = if ((Test-Path -LiteralPath $Path -PathType Container) -and $files.Count -gt 0) { 'PASS' } else { 'FAIL' }
    }
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $BaselineCommit) {
    throw "Wave 13 baseline mismatch. Expected $BaselineCommit, observed $head"
}
if (-not (Test-Path -LiteralPath $evidenceRoot -PathType Container)) {
    throw "Wave 12 evidence is missing for RunId ${RunId}: $evidenceRoot"
}
if (Test-Path -LiteralPath $kitRoot) {
    throw "Playtest kit output already exists; choose a fresh RunId instead of overwriting it: $kitRoot"
}
if (Test-Path -LiteralPath (Join-Path $evidenceRoot 'wave13-playtest-package.json')) {
    throw "Wave 13 package evidence already exists; choose a fresh RunId: $evidenceRoot"
}

$wave12 = Read-Json (Join-Path $evidenceRoot 'wave12-summary.json')
$build = Read-Json (Join-Path $evidenceRoot 'windows-development-build.json')
$smoke = Read-Json (Join-Path $evidenceRoot 'windows-hidden-smoke.json')
if ($null -eq $wave12 -or $wave12.baselineCommit -ne $BaselineCommit -or $wave12.infrastructureOverall -ne 'PASS' -or
    $wave12.compile -notmatch '^PASS' -or $wave12.windowsDevelopmentBuild -ne 'PASS' -or $wave12.hiddenSmoke -ne 'PASS') {
    throw 'Wave 12 compile/build/smoke infrastructure evidence is missing, mismatched, or not PASS.'
}
if ($null -eq $build -or $build.runId -ne $RunId -or $build.baselineCommit -ne $BaselineCommit -or
    $build.target -ne 'StandaloneWindows64' -or $build.result -ne 'Succeeded' -or $build.errors -ne 0) {
    throw 'Windows x64 Development build report is missing, mismatched, or unsuccessful.'
}
if ($null -eq $smoke -or $smoke.runId -ne $RunId -or $smoke.baselineCommit -ne $BaselineCommit -or
    $smoke.result -ne 'PASS' -or -not $smoke.aliveAtMinimum -or -not $smoke.respondingAtMinimum) {
    throw 'Hidden smoke evidence is missing, mismatched, or not PASS.'
}

$expectedBuildRoot = (Resolve-Path -LiteralPath $buildRoot).Path
$reportedExecutable = (Resolve-Path -LiteralPath ([string]$build.executable)).Path
$reportedBuildRoot = Split-Path -Parent $reportedExecutable
if (-not $reportedBuildRoot.Equals($expectedBuildRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Build report executable does not belong to this RunId work root. Expected $expectedBuildRoot, observed $reportedBuildRoot"
}

$executableName = Split-Path -Leaf $reportedExecutable
$dataDirectoryName = [System.IO.Path]::GetFileNameWithoutExtension($executableName) + '_Data'
$sourceRequired = @(
    [ordered]@{ name = 'executable'; path = $reportedExecutable; kind = 'file' },
    [ordered]@{ name = 'dataDirectory'; path = (Join-Path $buildRoot $dataDirectoryName); kind = 'directory' },
    [ordered]@{ name = 'unityPlayer'; path = (Join-Path $buildRoot 'UnityPlayer.dll'); kind = 'file' },
    [ordered]@{ name = 'monoBleedingEdge'; path = (Join-Path $buildRoot 'MonoBleedingEdge'); kind = 'directory' }
)
$missingSource = @($sourceRequired | Where-Object {
    if ($_.kind -eq 'file') { -not (Test-Path -LiteralPath $_.path -PathType Leaf) }
    else { -not (Test-Path -LiteralPath $_.path -PathType Container) }
})
if ($missingSource.Count -gt 0) {
    throw "Required Windows build components are missing: $([string]::Join(', ', @($missingSource | ForEach-Object { $_.name })))"
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
foreach ($item in Get-ChildItem -LiteralPath $buildRoot -Force) {
    Copy-Item -LiteralPath $item.FullName -Destination $packageRoot -Recurse -Force
}

$docs = @(
    [ordered]@{ source = 'Docs/QA/wave13-playtest-quick-start-ko.md'; target = 'PLAYTEST-QUICKSTART-KO.md' },
    [ordered]@{ source = 'Docs/QA/wave13-keyboard-mouse-checklist-ko.md'; target = 'CHECKLIST-KEYBOARD-MOUSE-KO.md' },
    [ordered]@{ source = 'Docs/QA/wave13-physical-gamepad-checklist-ko.md'; target = 'CHECKLIST-PHYSICAL-GAMEPAD-KO.md' }
)
foreach ($doc in $docs) {
    $sourcePath = Join-Path $projectRoot $doc.source
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { throw "Playtest document is missing: $($doc.source)" }
    Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $packageRoot $doc.target)
}

$payloadFiles = Get-PayloadFiles $packageRoot
$payloadDigestSource = [string]::Join("`n", @($payloadFiles | ForEach-Object { "$($_.sha256)  $($_.path)" }))
$packagedExecutable = Join-Path $packageRoot $executableName
$packagedUnityPlayer = Join-Path $packageRoot 'UnityPlayer.dll'
$requiredComponents = @(
    [ordered]@{
        name = 'executable'
        kind = 'file'
        path = $executableName
        exists = Test-Path -LiteralPath $packagedExecutable -PathType Leaf
        bytes = (Get-Item -LiteralPath $packagedExecutable).Length
        sha256 = Get-Sha256 $packagedExecutable
        sourceSha256 = Get-Sha256 $reportedExecutable
        status = if ((Get-Sha256 $packagedExecutable) -eq (Get-Sha256 $reportedExecutable)) { 'PASS' } else { 'FAIL' }
    },
    (Get-DirectoryContract 'dataDirectory' (Join-Path $packageRoot $dataDirectoryName) $dataDirectoryName $payloadFiles),
    [ordered]@{
        name = 'unityPlayer'
        kind = 'file'
        path = 'UnityPlayer.dll'
        exists = Test-Path -LiteralPath $packagedUnityPlayer -PathType Leaf
        bytes = (Get-Item -LiteralPath $packagedUnityPlayer).Length
        sha256 = Get-Sha256 $packagedUnityPlayer
        sourceSha256 = Get-Sha256 (Join-Path $buildRoot 'UnityPlayer.dll')
        status = if ((Get-Sha256 $packagedUnityPlayer) -eq (Get-Sha256 (Join-Path $buildRoot 'UnityPlayer.dll'))) { 'PASS' } else { 'FAIL' }
    },
    (Get-DirectoryContract 'monoBleedingEdge' (Join-Path $packageRoot 'MonoBleedingEdge') 'MonoBleedingEdge' $payloadFiles)
)

$manifest = [ordered]@{
    schemaVersion = 1
    title = 'Wave 13 Windows x64 Development playtest package manifest'
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedHead = $head
    createdUtc = [DateTime]::UtcNow.ToString('O')
    unityVersion = [string]$build.unityVersion
    buildTarget = [string]$build.target
    buildOptions = [string]$build.options
    sourceBuildRoot = $expectedBuildRoot
    packageRoot = $packageRoot
    exactBuildEvidence = 'windows-development-build.json + windows-hidden-smoke.json from the same RunId/baseline'
    requiredComponents = $requiredComponents
    payloadFileCount = $payloadFiles.Count
    payloadBytes = [long](($payloadFiles | Measure-Object -Property bytes -Sum).Sum)
    payloadAggregateSha256 = Get-StringSha256 $payloadDigestSource
    payloadFiles = $payloadFiles
    quickStart = 'PLAYTEST-QUICKSTART-KO.md'
    keyboardMouseChecklist = 'CHECKLIST-KEYBOARD-MOUSE-KO.md'
    physicalGamepadChecklist = 'CHECKLIST-PHYSICAL-GAMEPAD-KO.md'
    physicalGamepad = 'UNVERIFIED'
    steamReadiness = 'NOT_READY'
    overall = if (@($requiredComponents | Where-Object { $_.status -ne 'PASS' }).Count -eq 0) { 'PASS' } else { 'FAIL' }
}

$manifestPath = Join-Path $packageRoot 'PLAYTEST-MANIFEST.json'
[System.IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 12) + [Environment]::NewLine, $utf8NoBom)
$manifestEvidencePath = Join-Path $evidenceRoot 'wave13-playtest-package-manifest.json'
[System.IO.File]::WriteAllText($manifestEvidencePath, ($manifest | ConvertTo-Json -Depth 12) + [Environment]::NewLine, $utf8NoBom)

$sumLines = New-Object System.Collections.Generic.List[string]
foreach ($file in $payloadFiles) { $sumLines.Add("$($file.sha256)  $($file.path)") }
$sumLines.Add("$(Get-Sha256 $manifestPath)  PLAYTEST-MANIFEST.json")
$sumPath = Join-Path $packageRoot 'SHA256SUMS.txt'
[System.IO.File]::WriteAllLines($sumPath, $sumLines, $utf8NoBom)
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave13-playtest-package-sha256s.txt'), $sumLines, $utf8NoBom)

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($packageRoot, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entryNames = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
} finally {
    $zip.Dispose()
}
$zipRequired = [ordered]@{
    executable = $entryNames -contains $executableName
    dataDirectory = @($entryNames | Where-Object { $_.StartsWith($dataDirectoryName + '/', [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
    unityPlayer = $entryNames -contains 'UnityPlayer.dll'
    monoBleedingEdge = @($entryNames | Where-Object { $_.StartsWith('MonoBleedingEdge/', [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
    manifest = $entryNames -contains 'PLAYTEST-MANIFEST.json'
    sha256Sums = $entryNames -contains 'SHA256SUMS.txt'
    quickStart = $entryNames -contains 'PLAYTEST-QUICKSTART-KO.md'
    keyboardMouseChecklist = $entryNames -contains 'CHECKLIST-KEYBOARD-MOUSE-KO.md'
    physicalGamepadChecklist = $entryNames -contains 'CHECKLIST-PHYSICAL-GAMEPAD-KO.md'
}
$zipMissing = @($zipRequired.GetEnumerator() | Where-Object { -not $_.Value } | ForEach-Object { $_.Key })
$componentFailures = @($requiredComponents | Where-Object { $_.status -ne 'PASS' })
$overall = if ($componentFailures.Count -eq 0 -and $zipMissing.Count -eq 0 -and $manifest.overall -eq 'PASS') { 'PASS' } else { 'FAIL' }

$result = [ordered]@{
    schemaVersion = 1
    title = 'Wave 13 playtest distribution kit result'
    runId = $RunId
    baselineCommit = $BaselineCommit
    observedUtc = [DateTime]::UtcNow.ToString('O')
    overall = $overall
    wave12Overall = [string]$wave12.overall
    wave12Product = [string]$wave12.productOverall
    wave12Infrastructure = [string]$wave12.infrastructureOverall
    compile = [string]$wave12.compile
    windowsDevelopmentBuild = [string]$wave12.windowsDevelopmentBuild
    hiddenSmoke = [string]$wave12.hiddenSmoke
    addressables = [string]$wave12.addressables
    packageManifest = $manifestEvidencePath
    packageManifestSha256 = Get-Sha256 $manifestEvidencePath
    sha256Sums = Join-Path $evidenceRoot 'wave13-playtest-package-sha256s.txt'
    sha256SumsSha256 = Get-Sha256 (Join-Path $evidenceRoot 'wave13-playtest-package-sha256s.txt')
    packageZip = $zipPath
    packageZipBytes = (Get-Item -LiteralPath $zipPath).Length
    packageZipSha256 = Get-Sha256 $zipPath
    zipEntryCount = $entryNames.Count
    zipRequired = $zipRequired
    zipMissing = $zipMissing
    physicalGamepad = 'UNVERIFIED'
    physicalGamepadReason = 'No physical device and human actuation evidence was supplied. Synthetic gamepad coverage is not physical-device proof.'
    steamReadiness = 'NOT_READY'
    steamReadyClaim = $false
    exactRerun = "& '.\Assets\Editor\ParallelQA\New-Wave13PlaytestPackage.ps1' -RunId '$RunId' -BaselineCommit '$BaselineCommit'"
}
[System.IO.File]::WriteAllText((Join-Path $evidenceRoot 'wave13-playtest-package.json'), ($result | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8NoBom)
[System.IO.File]::WriteAllLines((Join-Path $evidenceRoot 'wave13-playtest-package.txt'), @(
    'Wave 13 playtest distribution kit'
    "Run ID: $RunId"
    "Baseline: $BaselineCommit"
    "Result: $overall"
    "Wave 12 product/infrastructure: $($result.wave12Product)/$($result.wave12Infrastructure)"
    "Compile/build/smoke: $($result.compile)/$($result.windowsDevelopmentBuild)/$($result.hiddenSmoke)"
    "Required components: $($requiredComponents.Count - $componentFailures.Count)/$($requiredComponents.Count) PASS"
    "ZIP required entries: $($zipRequired.Count - $zipMissing.Count)/$($zipRequired.Count) PASS"
    "ZIP: $zipPath"
    "ZIP SHA-256: $($result.packageZipSha256)"
    'Physical gamepad: UNVERIFIED'
    'Steam: NOT_READY (READY claim=false)'
), $utf8NoBom)

Write-Output "PLAYTEST_PACKAGE=$overall"
Write-Output "PACKAGE_ZIP=$zipPath"
Write-Output "PACKAGE_SHA256=$($result.packageZipSha256)"
Write-Output 'PHYSICAL_GAMEPAD=UNVERIFIED'
Write-Output 'STEAM=NOT_READY'
Write-Output "EVIDENCE=$evidenceRoot"
if ($overall -ne 'PASS') { exit 1 }
