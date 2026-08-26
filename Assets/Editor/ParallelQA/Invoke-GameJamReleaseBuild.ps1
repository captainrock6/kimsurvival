[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$')]
    [string]$RunId,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$BaselineCommit,

    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'The Game Jam release build requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$buildRoot = Join-Path $projectRoot 'Builds\WindowsReleaseVerification'
$sourceCommit = $BaselineCommit.ToLowerInvariant()
$shortCommit = $sourceCommit.Substring(0, 7)
$packageLeaf = "KimSurvivalIsland-gamejam-win64-release-$shortCommit"
$packageRoot = Join-Path $workRoot $packageLeaf
$zipPath = Join-Path $workRoot ($packageLeaf + '.zip')
$zipHashPath = $zipPath + '.sha256'
$buildLog = Join-Path $workRoot 'unity-release-build.log'
$stdoutLog = Join-Path $workRoot 'unity-release-build-stdout.log'
$stderrLog = Join-Path $workRoot 'unity-release-build-stderr.log'
$verificationText = Join-Path $projectRoot 'Artifacts\Verification\windows-release-log-verification-build.txt'
$utf8NoBom = New-Object Text.UTF8Encoding($false)

function Get-Sha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-StringSha256([string]$Value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($utf8NoBom.GetBytes($Value)))).Replace('-', '').ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
}

function Get-RelativePath([string]$BasePath, [string]$Path) {
    $baseWithSlash = $BasePath.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $baseUri = New-Object Uri($baseWithSlash)
    $pathUri = New-Object Uri($Path)
    return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace('\', '/')
}

function Quote-Argument([string]$Value) {
    if ($Value -match '[\s"]') { return '"' + $Value.Replace('"', '\"') + '"' }
    return $Value
}

function Write-Utf8([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, $utf8NoBom)
}

function Assert-ExactReleaseSource([string]$Stage) {
    $observedHead = (& git -C $projectRoot rev-parse HEAD).Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $observedHead -ne $sourceCommit) {
        throw "Release source commit mismatch $Stage. Expected $sourceCommit, observed $observedHead"
    }
    $status = @(& git -C $projectRoot status --porcelain=v1 --untracked-files=all -- `
        Assets Packages ProjectSettings `
        Docs/QA/gamejam-release-readme-ko.txt Docs/QA/gamejam-release-readme-en.txt `
        Docs/QA/gamejam-final-windows-candidate-30m-human-checklist-ko.md)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to verify release source cleanliness $Stage."
    }
    if ($status.Count -gt 0) {
        throw "Release source is not clean at the declared commit ${Stage}: $([string]::Join(' | ', $status))"
    }
    return $observedHead
}

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor not found: $UnityPath"
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}
if (Test-Path -LiteralPath $workRoot) {
    throw "Work directory already exists; choose a fresh RunId: $workRoot"
}
$head = Assert-ExactReleaseSource 'before Unity build'
$sourceCleanBeforeUnity = $true

New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
New-Item -ItemType Directory -Path $workRoot | Out-Null
$verificationParent = Split-Path -Parent $verificationText
if (-not (Test-Path -LiteralPath $verificationParent -PathType Container)) {
    New-Item -ItemType Directory -Path $verificationParent | Out-Null
}
if (Test-Path -LiteralPath $verificationText -PathType Leaf) {
    Remove-Item -LiteralPath $verificationText -Force
}
$startedUtc = [DateTime]::UtcNow
$arguments = @(
    '-batchmode', '-nographics', '-quit', '-projectPath', $projectRoot,
    '-executeMethod', 'KimSurvival.EditorTools.PrototypeProjectBuilder.BuildWindowsReleaseLogVerification',
    '-logFile', $buildLog
)
$argumentLine = [string]::Join(' ', @($arguments | ForEach-Object { Quote-Argument $_ }))
$process = Start-Process -FilePath $UnityPath -ArgumentList $argumentLine -WindowStyle Hidden -Wait -PassThru `
    -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog
if ($process.ExitCode -ne 0) {
    throw "Unity release build failed with exit code $($process.ExitCode). See $buildLog"
}
$head = Assert-ExactReleaseSource 'after Unity build and before packaging'
$sourceCleanAfterUnity = $true

$verificationReport = if (Test-Path -LiteralPath $verificationText -PathType Leaf) {
    Get-Content -LiteralPath $verificationText -Raw -Encoding UTF8
} else { '' }
$verificationTimestampValid = (Test-Path -LiteralPath $verificationText -PathType Leaf) -and
    (Get-Item -LiteralPath $verificationText).LastWriteTimeUtc -ge $startedUtc
if (-not $verificationTimestampValid -or
    $verificationReport -notmatch '(?m)^Result:\s*Succeeded\s*$' -or
    $verificationReport -notmatch '(?m)^BuildOptions:\s*None\s*$' -or
    $verificationReport -notmatch '(?m)^Development:\s*false\s*$') {
    throw "Unity release build did not produce successful verification text: $verificationText"
}

$executable = Join-Path $buildRoot 'KimSurvivalIsland.exe'
$dataRoot = Join-Path $buildRoot 'KimSurvivalIsland_Data'
$assembly = Join-Path $dataRoot 'Managed\Assembly-CSharp.dll'
$requiredFiles = @($executable, (Join-Path $buildRoot 'UnityPlayer.dll'), $assembly, (Join-Path $dataRoot 'boot.config'))
$missing = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) })
if ($missing.Count -gt 0 -or -not (Test-Path -LiteralPath (Join-Path $buildRoot 'MonoBleedingEdge') -PathType Container)) {
    throw "Release build is incomplete: $([string]::Join(', ', $missing))"
}

$forbiddenDebugPathPattern = '(?i)(?:^|[\\/])(?:[^\\/]*_)?(?:BackUpThisFolder_ButDontShipItWithYourGame|BurstDebugInformation_DoNotShip)(?:[\\/]|$)'
$forbiddenFiles = @(Get-ChildItem -LiteralPath $buildRoot -Recurse -File -Force | Where-Object {
    $_.Extension -match '^\.(pdb|mdb|dbg)$' -or $_.Name -eq 'WinPixEventRuntime.dll' -or
    $_.FullName -match $forbiddenDebugPathPattern
})
$forbiddenDirectories = @(Get-ChildItem -LiteralPath $buildRoot -Recurse -Directory -Force | Where-Object {
    $_.FullName -match $forbiddenDebugPathPattern
})
$bootConfigPath = Join-Path $dataRoot 'boot.config'
$bootText = Get-Content -LiteralPath $bootConfigPath -Raw -Encoding UTF8
$forbiddenBootSettings = @($bootText -split "`r?`n" | Where-Object {
    $_ -match '(?i)^\s*(development-player\s*=\s*1|player-connection-|profiler-|managed-debugger|wait-for-managed-debugger|wait-for-native-debugger\s*=\s*1|debugging-enabled\s*=\s*1)'
})
$privateAddresses = @([regex]::Matches(
    $bootText,
    '(?<!\d)(127\.0\.0\.1|10(?:\.\d{1,3}){3}|192\.168(?:\.\d{1,3}){2}|172\.(?:1[6-9]|2\d|3[01])(?:\.\d{1,3}){2})(?!\d)') |
    ForEach-Object { $_.Value } | Sort-Object -Unique)
if ($forbiddenFiles.Count -gt 0 -or $forbiddenDirectories.Count -gt 0 -or
    $forbiddenBootSettings.Count -gt 0 -or $privateAddresses.Count -gt 0) {
    throw 'Release hygiene failed: debug files, debug connection settings, or private addresses are present.'
}

New-Item -ItemType Directory -Path $packageRoot | Out-Null
foreach ($item in Get-ChildItem -LiteralPath $buildRoot -Force) {
    Copy-Item -LiteralPath $item.FullName -Destination $packageRoot -Recurse -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Docs\QA\gamejam-release-readme-ko.txt') `
    -Destination (Join-Path $packageRoot 'README-KO.txt')
Copy-Item -LiteralPath (Join-Path $projectRoot 'Docs\QA\gamejam-release-readme-en.txt') `
    -Destination (Join-Path $packageRoot 'README-EN.txt')
Copy-Item -LiteralPath (Join-Path $projectRoot 'Docs\QA\gamejam-final-windows-candidate-30m-human-checklist-ko.md') `
    -Destination (Join-Path $packageRoot 'QA-CHECKLIST-KO.md')

$packagedExecutable = Join-Path $packageRoot 'KimSurvivalIsland.exe'
$packagedAssembly = Join-Path $packageRoot 'KimSurvivalIsland_Data\Managed\Assembly-CSharp.dll'
$buildInfo = @(
    'Kim Survival Island GAME JAM release candidate'
    "Build source commit: $sourceCommit"
    "Packaged documentation commit: $head"
    'Build flavor: Release'
    'Build options: None'
    'Build target: StandaloneWindows64'
    'Executable: KimSurvivalIsland.exe'
    "Executable SHA-256: $(Get-Sha256 $packagedExecutable)"
    'Managed game assembly: KimSurvivalIsland_Data/Managed/Assembly-CSharp.dll'
    "Managed game assembly SHA-256: $(Get-Sha256 $packagedAssembly)"
    "Built UTC: $([DateTime]::UtcNow.ToString('O'))"
) -join [Environment]::NewLine
Write-Utf8 (Join-Path $packageRoot 'BUILD-INFO.txt') ($buildInfo + [Environment]::NewLine)

$payloadFiles = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Force | Sort-Object FullName)
$sumLines = @($payloadFiles | ForEach-Object { "$(Get-Sha256 $_.FullName)  $(Get-RelativePath $packageRoot $_.FullName)" })
Write-Utf8 (Join-Path $packageRoot 'SHA256SUMS.txt') ([string]::Join([Environment]::NewLine, $sumLines) + [Environment]::NewLine)
$packageDigestLines = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Force |
    ForEach-Object {
        [pscustomobject][ordered]@{
            path = Get-RelativePath $packageRoot $_.FullName
            sha256 = Get-Sha256 $_.FullName
        }
    } |
    Sort-Object path |
    ForEach-Object { "$($_.sha256)  $($_.path)" })
$packageFolderAggregateSha256 = Get-StringSha256 ([string]::Join("`n", $packageDigestLines))

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in @(Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Force | Sort-Object FullName)) {
        $entryName = $packageLeaf + '/' + (Get-RelativePath $packageRoot $file.FullName)
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal)
    }
} finally {
    $archive.Dispose()
}
$zipSha = Get-Sha256 $zipPath
Write-Utf8 $zipHashPath ("$zipSha  $([IO.Path]::GetFileName($zipPath))" + [Environment]::NewLine)

$summary = [ordered]@{
    schemaVersion = 1
    title = 'Game Jam Windows x64 non-development release candidate build'
    runId = $RunId
    baselineCommit = $sourceCommit
    observedHead = $head
    sourceCleanBeforeUnity = $sourceCleanBeforeUnity
    sourceCleanAfterUnity = $sourceCleanAfterUnity
    startedUtc = $startedUtc.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    unityPath = $UnityPath
    unityExitCode = $process.ExitCode
    buildOptions = 'None'
    buildRoot = $buildRoot
    packageFolder = $packageRoot
    packageFolderAggregateSha256 = $packageFolderAggregateSha256
    packageZip = $zipPath
    packageZipSha256 = $zipSha
    executableSha256 = Get-Sha256 $packagedExecutable
    managedAssemblySha256 = Get-Sha256 $packagedAssembly
    payloadFileCount = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Force).Count
    releaseHygiene = [ordered]@{
        forbiddenFiles = @($forbiddenFiles | ForEach-Object { Get-RelativePath $buildRoot $_.FullName })
        forbiddenDirectories = @($forbiddenDirectories | ForEach-Object { Get-RelativePath $buildRoot $_.FullName })
        forbiddenBootSettings = @($forbiddenBootSettings)
        privateAddresses = @($privateAddresses)
        status = 'PASS'
    }
    overall = 'PASS'
}
$summaryPath = Join-Path $evidenceRoot 'gamejam-release-build.json'
Write-Utf8 $summaryPath (($summary | ConvertTo-Json -Depth 10) + [Environment]::NewLine)
Write-Output 'RELEASE_BUILD=PASS'
Write-Output "PACKAGE_FOLDER=$packageRoot"
Write-Output "PACKAGE_ZIP=$zipPath"
Write-Output "ZIP_SHA256=$zipSha"
Write-Output "EVIDENCE=$summaryPath"
