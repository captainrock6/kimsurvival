[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$')]
    [string]$RunId,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$BaselineCommit,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$PackageSourceCommit,

    [Parameter(Mandatory = $true)]
    [string]$PackageFolder,

    [Parameter(Mandatory = $true)]
    [string]$PackageZip,

    [ValidateRange(6, 60)]
    [int]$MinimumSmokeSeconds = 6,

    [ValidateNotNullOrEmpty()]
    [string]$ManifestRelativePath = 'SHA256SUMS.txt',

    [ValidateNotNullOrEmpty()]
    [string]$BuildInfoRelativePath = 'BUILD-INFO.txt',

    [ValidateNotNullOrEmpty()]
    [string]$ExecutableRelativePath = 'KimSurvivalIsland.exe'
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw 'The Game Jam package integrity gate requires Windows PowerShell 5.1 or PowerShell 7+.'
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$evidenceRoot = Join-Path $projectRoot (Join-Path 'Artifacts\ParallelQA' $RunId)
$workRoot = Join-Path $projectRoot (Join-Path 'work\ParallelQA' $RunId)
$extractRoot = Join-Path $workRoot 'p'
$playerLog = Join-Path $workRoot 'extracted-player.log'
$capturedPlaytestLog = Join-Path $evidenceRoot 'extracted-development-playtest.jsonl'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$baselineCommitNormalized = $BaselineCommit.ToLowerInvariant()
$packageSourceCommitNormalized = $PackageSourceCommit.ToLowerInvariant()

function Write-Utf8NoBom([string]$Path, [string]$Value) {
    [IO.File]::WriteAllText($Path, $Value, $utf8NoBom)
}

function Write-Utf8Lines([string]$Path, [string[]]$Lines) {
    [IO.File]::WriteAllLines($Path, $Lines, $utf8NoBom)
}

function Get-Sha256([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
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

function Test-SameOrUnderPath([string]$Root, [string]$Path) {
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    if ($pathFull.Equals($rootFull, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    return $pathFull.StartsWith($rootFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Resolve-InputPath([string]$Value, [bool]$RequireDirectory) {
    $candidate = if ([IO.Path]::IsPathRooted($Value)) { $Value } else { Join-Path $projectRoot $Value }
    $resolved = (Resolve-Path -LiteralPath $candidate).Path
    $expectedType = if ($RequireDirectory) { 'Container' } else { 'Leaf' }
    if (-not (Test-Path -LiteralPath $resolved -PathType $expectedType)) {
        throw "Input path has the wrong type: $resolved"
    }
    return [IO.Path]::GetFullPath($resolved)
}

function ConvertTo-SafeRelativePath([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { throw 'An empty package-relative path is not allowed.' }
    $normalized = $Value.Replace('\', '/').Trim()
    if ($normalized.StartsWith('/') -or [IO.Path]::IsPathRooted($normalized) -or $normalized.Contains(':')) {
        throw "A rooted or drive-qualified package path is not allowed: $Value"
    }
    $parts = @($normalized.Split('/'))
    if ($parts.Count -eq 0 -or @($parts | Where-Object { $_ -eq '' -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
        throw "A package path contains an empty, current-directory, or parent-directory segment: $Value"
    }
    foreach ($part in $parts) {
        if ($part.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or $part.EndsWith('.') -or $part.EndsWith(' ')) {
            throw "A package path contains a Windows-invalid segment: $Value"
        }
        $deviceStem = $part.Split('.')[0]
        if ($deviceStem -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$') {
            throw "A package path contains a reserved Windows device name: $Value"
        }
    }
    return [string]::Join('/', $parts)
}

function Resolve-ContainedPath([string]$Root, [string]$RelativePath) {
    $safeRelative = ConvertTo-SafeRelativePath $RelativePath
    $nativeRelative = $safeRelative.Replace('/', [IO.Path]::DirectorySeparatorChar)
    $fullPath = [IO.Path]::GetFullPath((Join-Path $Root $nativeRelative))
    if (-not (Test-SameOrUnderPath $Root $fullPath)) {
        throw "Resolved package path escaped its exact root: $RelativePath"
    }
    return $fullPath
}

function Assert-NoReparsePoints([string]$Root) {
    $rootItem = Get-Item -LiteralPath $Root -Force
    if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "A reparse-point package root is not accepted: $Root"
    }
    $reparse = @(Get-ChildItem -LiteralPath $Root -Recurse -Force | Where-Object {
        ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0
    })
    if ($reparse.Count -gt 0) {
        throw "Package trees containing reparse points are not accepted: $($reparse[0].FullName)"
    }
}

function Get-DirectorySnapshot([string]$Root) {
    Assert-NoReparsePoints $Root
    $directoryCount = @(Get-ChildItem -LiteralPath $Root -Recurse -Directory -Force).Count
    $files = @(Get-ChildItem -LiteralPath $Root -Recurse -File -Force | ForEach-Object {
        [pscustomobject][ordered]@{
            path = Get-RelativePath $Root $_.FullName
            bytes = [long]$_.Length
            sha256 = Get-Sha256 $_.FullName
        }
    } | Sort-Object path)
    $digestSource = [string]::Join("`n", @($files | ForEach-Object { "$($_.sha256)  $($_.path)" }))
    return [pscustomobject][ordered]@{
        root = $Root
        fileCount = $files.Count
        directoryCount = $directoryCount
        bytes = [long](($files | Measure-Object -Property bytes -Sum).Sum)
        aggregateSha256 = Get-StringSha256 $digestSource
        files = $files
    }
}

function Get-InternalManifestAudit(
    [string]$Root,
    [string]$ManifestPathRelative,
    [string]$BuildInfoPathRelative,
    [string]$ExpectedExecutableRelative,
    [string]$ExpectedSourceCommit
) {
    $issues = New-Object Collections.Generic.List[string]
    $entries = New-Object Collections.Generic.List[object]
    $seen = @{}
    $safeManifestRelative = ''
    $manifestPath = ''
    try {
        $safeManifestRelative = ConvertTo-SafeRelativePath $ManifestPathRelative
        $manifestPath = Resolve-ContainedPath $Root $safeManifestRelative
    } catch {
        $issues.Add($_.Exception.Message)
    }

    if ([string]::IsNullOrWhiteSpace($manifestPath) -or -not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        $issues.Add("Internal SHA-256 manifest is missing: $ManifestPathRelative")
    } else {
        $lineNumber = 0
        foreach ($line in [IO.File]::ReadAllLines($manifestPath, [Text.Encoding]::UTF8)) {
            $lineNumber++
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            $match = [regex]::Match($line, '^(?<sha>[0-9a-fA-F]{64})[ \t]{2,}(?<path>.+)$')
            if (-not $match.Success) {
                $issues.Add("Manifest line $lineNumber does not use '<sha256><two spaces><path>'.")
                continue
            }
            $expectedSha = $match.Groups['sha'].Value.ToLowerInvariant()
            $rawPath = $match.Groups['path'].Value
            try {
                $relativePath = ConvertTo-SafeRelativePath $rawPath
                if ($relativePath.Equals($safeManifestRelative, [StringComparison]::OrdinalIgnoreCase)) {
                    throw 'The SHA-256 manifest cannot contain a self-hash entry.'
                }
                if ($seen.ContainsKey($relativePath)) {
                    throw "Duplicate case-insensitive manifest entry: $relativePath"
                }
                $seen[$relativePath] = $true
                $fullPath = Resolve-ContainedPath $Root $relativePath
                $exists = Test-Path -LiteralPath $fullPath -PathType Leaf
                $actualSha = if ($exists) { Get-Sha256 $fullPath } else { '' }
                $status = if ($exists -and $actualSha -eq $expectedSha) { 'PASS' } else { 'FAIL' }
                if ($status -ne 'PASS') {
                    $issues.Add("Manifest hash mismatch or missing file: $relativePath")
                }
                $entries.Add([pscustomobject][ordered]@{
                    path = $relativePath
                    expectedSha256 = $expectedSha
                    actualSha256 = $actualSha
                    exists = $exists
                    status = $status
                })
            } catch {
                $issues.Add("Manifest line ${lineNumber}: $($_.Exception.Message)")
            }
        }
    }

    $snapshot = Get-DirectorySnapshot $Root
    $actualPayloadPaths = @($snapshot.files | Where-Object {
        -not $_.path.Equals($safeManifestRelative, [StringComparison]::OrdinalIgnoreCase)
    } | ForEach-Object { $_.path })
    $unlisted = @($actualPayloadPaths | Where-Object { -not $seen.ContainsKey($_) } | Sort-Object)
    foreach ($path in $unlisted) { $issues.Add("Package file is not listed by the internal manifest: $path") }

    $buildInfoRelative = ConvertTo-SafeRelativePath $BuildInfoPathRelative
    $buildInfoPath = Resolve-ContainedPath $Root $buildInfoRelative
    $buildInfoExists = Test-Path -LiteralPath $buildInfoPath -PathType Leaf
    $buildInfoSourceCommit = ''
    $packagedDocumentationCommit = ''
    $declaredExecutableRelative = ''
    $declaredExecutableSha256 = ''
    $declaredManagedAssemblyRelative = ''
    $declaredManagedAssemblySha256 = ''
    $buildInfoParseError = ''
    if ($buildInfoExists) {
        try {
            $buildInfo = Get-Content -LiteralPath $buildInfoPath -Raw -Encoding UTF8
            $sourceMatch = [regex]::Match($buildInfo, '(?im)^Build source commit:\s*(?<value>[0-9a-f]{40})\s*$')
            $docsMatch = [regex]::Match($buildInfo, '(?im)^Packaged documentation commit:\s*(?<value>[0-9a-f]{40})\s*$')
            $executableMatch = [regex]::Match($buildInfo, '(?im)^Executable:\s*(?<value>[^\r\n]+?)\s*$')
            $executableShaMatch = [regex]::Match($buildInfo, '(?im)^Executable SHA-256:\s*(?<value>[0-9a-f]{64})\s*$')
            $assemblyMatch = [regex]::Match($buildInfo, '(?im)^Managed game assembly:\s*(?<value>[^\r\n]+?)\s*$')
            $assemblyShaMatch = [regex]::Match($buildInfo, '(?im)^Managed game assembly SHA-256:\s*(?<value>[0-9a-f]{64})\s*$')
            if (-not ($sourceMatch.Success -and $docsMatch.Success -and $executableMatch.Success -and
                $executableShaMatch.Success -and $assemblyMatch.Success -and $assemblyShaMatch.Success)) {
                throw 'BUILD-INFO.txt is missing one or more required candidate identity fields.'
            }
            $buildInfoSourceCommit = $sourceMatch.Groups['value'].Value.ToLowerInvariant()
            $packagedDocumentationCommit = $docsMatch.Groups['value'].Value.ToLowerInvariant()
            $declaredExecutableRelative = ConvertTo-SafeRelativePath $executableMatch.Groups['value'].Value
            $declaredExecutableSha256 = $executableShaMatch.Groups['value'].Value.ToLowerInvariant()
            $declaredManagedAssemblyRelative = ConvertTo-SafeRelativePath $assemblyMatch.Groups['value'].Value
            $declaredManagedAssemblySha256 = $assemblyShaMatch.Groups['value'].Value.ToLowerInvariant()
        } catch {
            $buildInfoParseError = $_.Exception.Message
            $issues.Add("BUILD-INFO.txt could not be parsed: $buildInfoParseError")
        }
    } else {
        $issues.Add("Candidate build information is missing: $buildInfoRelative")
    }

    $buildInfoSourceMatches = $buildInfoExists -and [string]::IsNullOrWhiteSpace($buildInfoParseError) -and
        $buildInfoSourceCommit.Equals($ExpectedSourceCommit, [StringComparison]::OrdinalIgnoreCase)
    if ($buildInfoExists -and [string]::IsNullOrWhiteSpace($buildInfoParseError) -and -not $buildInfoSourceMatches) {
        $issues.Add("Package source commit mismatch. Expected $ExpectedSourceCommit, observed $buildInfoSourceCommit")
    }
    $expectedExecutableNormalized = ConvertTo-SafeRelativePath $ExpectedExecutableRelative
    $executablePath = Resolve-ContainedPath $Root $expectedExecutableNormalized
    $executableSha256 = Get-Sha256 $executablePath
    $managedAssemblyPath = if ([string]::IsNullOrWhiteSpace($declaredManagedAssemblyRelative)) {
        ''
    } else {
        Resolve-ContainedPath $Root $declaredManagedAssemblyRelative
    }
    $managedAssemblySha256 = Get-Sha256 $managedAssemblyPath
    $identityMatches = [string]::IsNullOrWhiteSpace($buildInfoParseError) -and
        $declaredExecutableRelative.Equals($expectedExecutableNormalized, [StringComparison]::OrdinalIgnoreCase) -and
        $executableSha256 -eq $declaredExecutableSha256 -and
        -not [string]::IsNullOrWhiteSpace($managedAssemblySha256) -and
        $managedAssemblySha256 -eq $declaredManagedAssemblySha256
    if ($buildInfoExists -and [string]::IsNullOrWhiteSpace($buildInfoParseError) -and -not $identityMatches) {
        $issues.Add('BUILD-INFO.txt executable or managed assembly identity does not match the packaged files.')
    }

    return [pscustomobject][ordered]@{
        schemaVersion = 1
        root = $Root
        manifestRelativePath = $safeManifestRelative
        manifestPath = $manifestPath
        manifestExists = Test-Path -LiteralPath $manifestPath -PathType Leaf
        manifestSha256 = Get-Sha256 $manifestPath
        manifestEntryCount = $entries.Count
        packagePayloadFileCount = $actualPayloadPaths.Count
        exactFileSet = $unlisted.Count -eq 0 -and $entries.Count -eq $actualPayloadPaths.Count
        entries = $entries.ToArray()
        unlistedFiles = $unlisted
        buildInfo = [ordered]@{
            path = $buildInfoRelative
            exists = $buildInfoExists
            sourceCommit = $buildInfoSourceCommit
            sourceCommitMatches = $buildInfoSourceMatches
            packagedDocumentationCommit = $packagedDocumentationCommit
            executableRelativePath = $declaredExecutableRelative
            executableSha256 = $executableSha256
            executableSha256Matches = $executableSha256 -eq $declaredExecutableSha256
            managedAssemblyRelativePath = $declaredManagedAssemblyRelative
            managedAssemblySha256 = $managedAssemblySha256
            managedAssemblySha256Matches = $managedAssemblySha256 -eq $declaredManagedAssemblySha256
            identityMatches = $identityMatches
            parseError = $buildInfoParseError
        }
        issues = $issues.ToArray()
        status = if ($issues.Count -eq 0 -and $entries.Count -eq $actualPayloadPaths.Count) { 'PASS' } else { 'FAIL' }
    }
}

function Expand-SafePackageZip(
    [string]$ZipPath,
    [string]$Destination,
    [string]$ExpectedTopLevelDirectory,
    [int]$MaximumFiles,
    [int]$MaximumEntries,
    [long]$MaximumBytes
) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path -LiteralPath $Destination) {
        throw "Extraction destination already exists: $Destination"
    }
    New-Item -ItemType Directory -Path $Destination | Out-Null
    $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    $seen = @{}
    $records = New-Object Collections.Generic.List[object]
    $expandedBytes = [long]0
    $safeTopLevel = ConvertTo-SafeRelativePath $ExpectedTopLevelDirectory
    if ($safeTopLevel.Contains('/')) {
        throw "The expected ZIP top-level directory must be one path segment: $ExpectedTopLevelDirectory"
    }
    $requiredPrefix = $safeTopLevel + '/'
    $observedEntries = 0
    try {
        foreach ($entry in $archive.Entries) {
            $observedEntries++
            if ($observedEntries -gt $MaximumEntries) {
                throw "ZIP entry count exceeds the bounded candidate-folder allowance of $MaximumEntries."
            }
            $rawName = $entry.FullName.Replace('\', '/')
            if ($rawName.EndsWith('/')) {
                $directoryName = $rawName.TrimEnd('/')
                if ([string]::IsNullOrWhiteSpace($directoryName)) {
                    throw 'ZIP contains an empty rooted directory entry.'
                }
                $safeDirectoryName = ConvertTo-SafeRelativePath $directoryName
                if (-not ($safeDirectoryName.Equals($safeTopLevel, [StringComparison]::OrdinalIgnoreCase) -or
                    $safeDirectoryName.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase))) {
                    throw "ZIP directory is outside the expected single top-level candidate folder: $directoryName"
                }
                $directoryUnixFileType = (($entry.ExternalAttributes -shr 16) -band 0xF000)
                if ($directoryUnixFileType -eq 0xA000) {
                    throw "ZIP symbolic-link entries are not accepted: $directoryName"
                }
                continue
            }
            $relativePath = ConvertTo-SafeRelativePath $rawName
            if (-not $relativePath.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "ZIP file is outside the expected single top-level candidate folder: $relativePath"
            }
            $payloadRelativePath = ConvertTo-SafeRelativePath $relativePath.Substring($requiredPrefix.Length)
            if ($seen.ContainsKey($relativePath)) {
                throw "ZIP contains a duplicate case-insensitive file entry: $relativePath"
            }
            $seen[$relativePath] = $true
            if ($seen.Count -gt $MaximumFiles) {
                throw "ZIP file count exceeds the exact package-folder count of $MaximumFiles."
            }
            if ([long]$entry.Length -gt ($MaximumBytes - $expandedBytes)) {
                throw "ZIP expanded bytes exceed the exact package-folder byte count of $MaximumBytes."
            }
            $expandedBytes += [long]$entry.Length
            $unixFileType = (($entry.ExternalAttributes -shr 16) -band 0xF000)
            if ($unixFileType -eq 0xA000) {
                throw "ZIP symbolic-link entries are not accepted: $relativePath"
            }
            $destinationPath = Resolve-ContainedPath $Destination $payloadRelativePath
            $destinationDirectory = Split-Path -Parent $destinationPath
            if (-not (Test-Path -LiteralPath $destinationDirectory -PathType Container)) {
                New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
            }
            $input = $entry.Open()
            try {
                $output = New-Object IO.FileStream($destinationPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
                try {
                    $buffer = New-Object byte[] 65536
                    $entryCopiedBytes = [long]0
                    while (($readBytes = $input.Read($buffer, 0, $buffer.Length)) -gt 0) {
                        if ($entryCopiedBytes + $readBytes -gt [long]$entry.Length -or
                            $expandedBytes - [long]$entry.Length + $entryCopiedBytes + $readBytes -gt $MaximumBytes) {
                            throw "ZIP entry expanded beyond its declared or package-bounded size: $relativePath"
                        }
                        $output.Write($buffer, 0, $readBytes)
                        $entryCopiedBytes += $readBytes
                    }
                    if ($entryCopiedBytes -ne [long]$entry.Length) {
                        throw "ZIP entry expanded length differs from its central-directory declaration: $relativePath"
                    }
                } finally { $output.Dispose() }
            } finally {
                $input.Dispose()
            }
            $records.Add([pscustomobject][ordered]@{
                zipPath = $relativePath
                path = $payloadRelativePath
                compressedBytes = [long]$entry.CompressedLength
                bytes = [long]$entry.Length
            })
        }
    } finally {
        $archive.Dispose()
    }
    return $records.ToArray()
}

function Compare-DirectorySnapshots($Expected, $Actual) {
    $expectedMap = @{}
    foreach ($file in $Expected.files) { $expectedMap[[string]$file.path] = $file }
    $actualMap = @{}
    foreach ($file in $Actual.files) { $actualMap[[string]$file.path] = $file }

    $missing = @($expectedMap.Keys | Where-Object { -not $actualMap.ContainsKey($_) } | Sort-Object)
    $extra = @($actualMap.Keys | Where-Object { -not $expectedMap.ContainsKey($_) } | Sort-Object)
    $mismatches = New-Object Collections.Generic.List[object]
    foreach ($path in @($expectedMap.Keys | Where-Object { $actualMap.ContainsKey($_) } | Sort-Object)) {
        $left = $expectedMap[$path]
        $right = $actualMap[$path]
        if ([long]$left.bytes -ne [long]$right.bytes -or [string]$left.sha256 -ne [string]$right.sha256) {
            $mismatches.Add([pscustomobject][ordered]@{
                path = $path
                folderBytes = [long]$left.bytes
                extractedBytes = [long]$right.bytes
                folderSha256 = [string]$left.sha256
                extractedSha256 = [string]$right.sha256
            })
        }
    }
    return [pscustomobject][ordered]@{
        expectedFileCount = $Expected.fileCount
        actualFileCount = $Actual.fileCount
        expectedAggregateSha256 = $Expected.aggregateSha256
        actualAggregateSha256 = $Actual.aggregateSha256
        missingFromExtracted = $missing
        extraInExtracted = $extra
        contentMismatches = $mismatches.ToArray()
        status = if ($missing.Count -eq 0 -and $extra.Count -eq 0 -and $mismatches.Count -eq 0 -and
            $Expected.aggregateSha256 -eq $Actual.aggregateSha256) { 'PASS' } else { 'FAIL' }
    }
}

function Invoke-ExtractedHiddenSmoke(
    [string]$Root,
    [string]$ExecutablePathRelative,
    [int]$RequiredSeconds,
    [string]$LogPath,
    [string]$CapturedPlaytestLogPath
) {
    $startedUtc = [DateTime]::UtcNow
    $safeExecutableRelative = ''
    $executable = ''
    $process = $null
    $processId = 0
    $earlyExit = $false
    $exitCode = $null
    $aliveAtMinimum = $false
    $respondingAtMinimum = $false
    $respondingByGraceDeadline = $false
    $launchError = ''
    $responseGraceSeconds = 6
    $terminatedByRunner = $false
    $cleanupSucceeded = $false
    $residualPackageProcessIds = @()
    $developmentPlaytestLog = ''
    $developmentPlaytestLogCaptured = $false
    $observedSeconds = 0.0
    try {
        $safeExecutableRelative = ConvertTo-SafeRelativePath $ExecutablePathRelative
        $executable = Resolve-ContainedPath $Root $safeExecutableRelative
        if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
            throw "Extracted executable is missing: $safeExecutableRelative"
        }
        $arguments = "-screen-width 1280 -screen-height 800 -screen-fullscreen 0 -logFile `"$LogPath`""
        $process = Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory $Root -WindowStyle Hidden -PassThru
        $processId = $process.Id
        $watch = [Diagnostics.Stopwatch]::StartNew()
        while ($watch.Elapsed.TotalSeconds -lt $RequiredSeconds) {
            Start-Sleep -Milliseconds 250
            $process.Refresh()
            if ($process.HasExited) {
                $earlyExit = $true
                $exitCode = $process.ExitCode
                break
            }
        }
        $watch.Stop()
        $observedSeconds = $watch.Elapsed.TotalSeconds
        $process.Refresh()
        if (-not $process.HasExited) {
            $aliveAtMinimum = $observedSeconds -ge $RequiredSeconds
            $respondingAtMinimum = $process.Responding
            $respondingByGraceDeadline = $respondingAtMinimum
            $responseWatch = [Diagnostics.Stopwatch]::StartNew()
            while (-not $respondingByGraceDeadline -and $responseWatch.Elapsed.TotalSeconds -lt $responseGraceSeconds) {
                Start-Sleep -Milliseconds 250
                $process.Refresh()
                if ($process.HasExited) {
                    $earlyExit = $true
                    $exitCode = $process.ExitCode
                    break
                }
                $respondingByGraceDeadline = $process.Responding
            }
            $responseWatch.Stop()
        }
    } catch {
        $launchError = $_.Exception.Message
    } finally {
        if ($null -ne $process) {
            try {
                $process.Refresh()
                if (-not $process.HasExited) {
                    Stop-Process -Id $process.Id -Force
                    $process.WaitForExit(5000) | Out-Null
                    $process.Refresh()
                    $terminatedByRunner = $process.HasExited
                }
            } catch {
                if ([string]::IsNullOrWhiteSpace($launchError)) { $launchError = $_.Exception.Message }
            }
        }
        try {
            $packageProcesses = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
                try {
                    -not [string]::IsNullOrWhiteSpace($_.Path) -and (Test-SameOrUnderPath $Root $_.Path)
                } catch {
                    $false
                }
            })
            foreach ($packageProcess in $packageProcesses) {
                Stop-Process -Id $packageProcess.Id -Force -ErrorAction Stop
                $packageProcess.WaitForExit(5000) | Out-Null
            }
            $residualPackageProcessIds = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
                try {
                    -not [string]::IsNullOrWhiteSpace($_.Path) -and (Test-SameOrUnderPath $Root $_.Path)
                } catch {
                    $false
                }
            } | ForEach-Object { $_.Id })
            $cleanupSucceeded = $residualPackageProcessIds.Count -eq 0
        } catch {
            $cleanupSucceeded = $false
            if ([string]::IsNullOrWhiteSpace($launchError)) { $launchError = "Package process cleanup failed: $($_.Exception.Message)" }
        }
    }
    try {
        if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
            $playerLogText = Get-Content -LiteralPath $LogPath -Raw -Encoding UTF8
            $playtestLogMatch = [regex]::Match($playerLogText, '(?m)^\[Kim Survival Playtest\] Development-only local JSONL:\s*(?<path>.+?)\s*$')
            if ($playtestLogMatch.Success) {
                $developmentPlaytestLog = $playtestLogMatch.Groups['path'].Value.Trim()
                if (Test-Path -LiteralPath $developmentPlaytestLog -PathType Leaf) {
                    [IO.File]::Copy($developmentPlaytestLog, $CapturedPlaytestLogPath, $false)
                    $developmentPlaytestLogCaptured = Test-Path -LiteralPath $CapturedPlaytestLogPath -PathType Leaf
                }
            }
        }
    } catch {
        if ([string]::IsNullOrWhiteSpace($launchError)) {
            $launchError = "Development playtest log capture failed: $($_.Exception.Message)"
        }
    }
    $status = if ([string]::IsNullOrWhiteSpace($launchError) -and -not $earlyExit -and
        $aliveAtMinimum -and $respondingByGraceDeadline -and $cleanupSucceeded -and
        $developmentPlaytestLogCaptured) { 'PASS' } else { 'FAIL' }
    return [pscustomobject][ordered]@{
        schemaVersion = 1
        startedUtc = $startedUtc.ToString('O')
        completedUtc = [DateTime]::UtcNow.ToString('O')
        extractedRoot = $Root
        executableRelativePath = $safeExecutableRelative
        executable = $executable
        executableExists = Test-Path -LiteralPath $executable -PathType Leaf
        executableSha256 = Get-Sha256 $executable
        playerLog = $LogPath
        minimumSeconds = $RequiredSeconds
        observedSeconds = [Math]::Round($observedSeconds, 3)
        resolution = '1280x800 windowed'
        windowStyle = 'Hidden'
        processId = $processId
        aliveAtMinimum = $aliveAtMinimum
        respondingAtMinimum = $respondingAtMinimum
        responseGraceSeconds = $responseGraceSeconds
        respondingByGraceDeadline = $respondingByGraceDeadline
        earlyExit = $earlyExit
        exitCode = $exitCode
        terminatedByRunner = $terminatedByRunner
        cleanupSucceeded = $cleanupSucceeded
        residualPackageProcessIds = @($residualPackageProcessIds)
        developmentPlaytestLog = $developmentPlaytestLog
        developmentPlaytestLogCaptured = $developmentPlaytestLogCaptured
        capturedPlaytestLog = $CapturedPlaytestLogPath
        launchError = $launchError
        status = $status
    }
}

$packageFolderPath = Resolve-InputPath $PackageFolder $true
$packageZipPath = Resolve-InputPath $PackageZip $false
$manifestRelativeNormalized = ConvertTo-SafeRelativePath $ManifestRelativePath
$buildInfoRelativeNormalized = ConvertTo-SafeRelativePath $BuildInfoRelativePath
$executableRelativeNormalized = ConvertTo-SafeRelativePath $ExecutableRelativePath
$packageTopLevelDirectory = Split-Path -Leaf $packageFolderPath
$packageTopLevelDirectory = ConvertTo-SafeRelativePath $packageTopLevelDirectory
if (-not [IO.Path]::GetExtension($packageZipPath).Equals('.zip', [StringComparison]::OrdinalIgnoreCase)) {
    throw "PackageZip must be an exact .zip file path: $packageZipPath"
}
$zipItem = Get-Item -LiteralPath $packageZipPath -Force
if (($zipItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "A reparse-point PackageZip is not accepted: $packageZipPath"
}
Assert-NoReparsePoints $packageFolderPath
if (Test-SameOrUnderPath $packageFolderPath $packageZipPath) {
    throw 'PackageZip must not reside inside PackageFolder; otherwise exact folder/ZIP comparison is recursive or ambiguous.'
}
if ((Test-SameOrUnderPath $packageFolderPath $evidenceRoot) -or (Test-SameOrUnderPath $packageFolderPath $workRoot)) {
    throw 'PackageFolder must not contain this run output; the runner only writes below the fresh evidence/work roots.'
}
if (Test-Path -LiteralPath $evidenceRoot) {
    throw "Evidence directory already exists; choose a fresh RunId: $evidenceRoot"
}
if (Test-Path -LiteralPath $workRoot) {
    throw "Work directory already exists; choose a fresh RunId: $workRoot"
}

$head = (& git -C $projectRoot rev-parse HEAD).Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0 -or $head -ne $baselineCommitNormalized) {
    throw "QA baseline commit mismatch. Expected $baselineCommitNormalized, observed $head"
}

New-Item -ItemType Directory -Path $evidenceRoot | Out-Null
New-Item -ItemType Directory -Path $workRoot | Out-Null
$startedUtc = [DateTime]::UtcNow
$folderBefore = Get-DirectorySnapshot $packageFolderPath
$zipSha256Before = Get-Sha256 $packageZipPath
$folderManifestAudit = Get-InternalManifestAudit $packageFolderPath $manifestRelativeNormalized `
    $buildInfoRelativeNormalized $executableRelativeNormalized $packageSourceCommitNormalized

$folderManifestReport = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $baselineCommitNormalized
    packageSourceCommit = $packageSourceCommitNormalized
    observedHead = $head
    observedUtc = [DateTime]::UtcNow.ToString('O')
    packageFolder = $packageFolderPath
    packageFolderFileCount = $folderBefore.fileCount
    packageFolderBytes = $folderBefore.bytes
    packageFolderAggregateSha256 = $folderBefore.aggregateSha256
    audit = $folderManifestAudit
    overall = $folderManifestAudit.status
}
Write-Utf8NoBom (Join-Path $evidenceRoot 'gamejam-package-folder-manifest.json') (($folderManifestReport | ConvertTo-Json -Depth 16) + [Environment]::NewLine)
Write-Utf8Lines (Join-Path $evidenceRoot 'gamejam-package-folder-manifest.txt') @(
    'Game Jam package folder internal SHA-256 manifest audit'
    "Run ID: $RunId"
    "QA baseline / observed HEAD: $baselineCommitNormalized / $head"
    "Package source commit: $packageSourceCommitNormalized"
    "Package folder: $packageFolderPath"
    "Folder files: $($folderBefore.fileCount)"
    "Manifest entries: $($folderManifestAudit.manifestEntryCount)"
    "Exact manifest file set: $($folderManifestAudit.exactFileSet)"
    "BUILD-INFO source commit matches: $($folderManifestAudit.buildInfo.sourceCommitMatches)"
    "Executable / managed assembly identity matches: $($folderManifestAudit.buildInfo.identityMatches)"
    "Result: $($folderManifestAudit.status)"
    "Issues: $([string]::Join(' | ', @($folderManifestAudit.issues)))"
)

$extractionError = ''
$zipEntries = @()
$extractedSnapshot = $null
$extractedPackageRoot = ''
$extractedManifestAudit = $null
$comparison = $null
try {
    $maximumZipEntries = $folderBefore.fileCount + $folderBefore.directoryCount + 1
    $zipEntries = @(Expand-SafePackageZip $packageZipPath $extractRoot $packageTopLevelDirectory `
        $folderBefore.fileCount $maximumZipEntries $folderBefore.bytes)
    $extractedPackageRoot = $extractRoot
    if (-not (Test-Path -LiteralPath $extractedPackageRoot -PathType Container)) {
        throw "ZIP did not produce the exact expected top-level candidate folder: $packageTopLevelDirectory"
    }
    $extractedSnapshot = Get-DirectorySnapshot $extractedPackageRoot
    $extractedManifestAudit = Get-InternalManifestAudit $extractedPackageRoot $manifestRelativeNormalized `
        $buildInfoRelativeNormalized $executableRelativeNormalized $packageSourceCommitNormalized
    $comparison = Compare-DirectorySnapshots $folderBefore $extractedSnapshot
} catch {
    $extractionError = $_.Exception.Message
    $comparison = [pscustomobject][ordered]@{
        expectedFileCount = $folderBefore.fileCount
        actualFileCount = if ($null -eq $extractedSnapshot) { 0 } else { $extractedSnapshot.fileCount }
        expectedAggregateSha256 = $folderBefore.aggregateSha256
        actualAggregateSha256 = if ($null -eq $extractedSnapshot) { '' } else { $extractedSnapshot.aggregateSha256 }
        missingFromExtracted = @()
        extraInExtracted = @()
        contentMismatches = @()
        status = 'FAIL'
    }
}

$zipComparisonOverall = if ([string]::IsNullOrWhiteSpace($extractionError) -and
    $comparison.status -eq 'PASS' -and $null -ne $extractedManifestAudit -and
    $extractedManifestAudit.status -eq 'PASS') { 'PASS' } else { 'FAIL' }
$zipComparisonReport = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $baselineCommitNormalized
    packageSourceCommit = $packageSourceCommitNormalized
    observedUtc = [DateTime]::UtcNow.ToString('O')
    packageFolder = $packageFolderPath
    packageZip = $packageZipPath
    packageZipBytes = (Get-Item -LiteralPath $packageZipPath).Length
    packageZipSha256 = $zipSha256Before
    extractionContainer = $extractRoot
    extractedPackageRoot = $extractedPackageRoot
    safeExtractedFileCount = $zipEntries.Count
    extractionError = $extractionError
    comparison = $comparison
    extractedManifest = $extractedManifestAudit
    overall = $zipComparisonOverall
}
Write-Utf8NoBom (Join-Path $evidenceRoot 'gamejam-package-zip-folder-comparison.json') (($zipComparisonReport | ConvertTo-Json -Depth 16) + [Environment]::NewLine)
Write-Utf8Lines (Join-Path $evidenceRoot 'gamejam-package-zip-folder-comparison.txt') @(
    'Game Jam package ZIP versus folder exact comparison'
    "Run ID: $RunId"
    "QA baseline: $baselineCommitNormalized"
    "Package source commit: $packageSourceCommitNormalized"
    "Package ZIP: $packageZipPath"
    "Package ZIP SHA-256: $zipSha256Before"
    "Safely extracted files: $($zipEntries.Count)"
    "Folder/extracted file counts: $($comparison.expectedFileCount)/$($comparison.actualFileCount)"
    "Missing/extra/content mismatch: $(@($comparison.missingFromExtracted).Count)/$(@($comparison.extraInExtracted).Count)/$(@($comparison.contentMismatches).Count)"
    "Extracted internal manifest: $(if ($null -eq $extractedManifestAudit) { 'FAIL' } else { $extractedManifestAudit.status })"
    "Extraction error: $extractionError"
    "Result: $zipComparisonOverall"
)

$smoke = if ($folderManifestAudit.status -eq 'PASS' -and $zipComparisonOverall -eq 'PASS') {
    Invoke-ExtractedHiddenSmoke $extractedPackageRoot $executableRelativeNormalized $MinimumSmokeSeconds `
        $playerLog $capturedPlaytestLog
} else {
    [pscustomobject][ordered]@{
        schemaVersion = 1
        startedUtc = [DateTime]::UtcNow.ToString('O')
        completedUtc = [DateTime]::UtcNow.ToString('O')
        extractedRoot = $extractedPackageRoot
        executableRelativePath = $executableRelativeNormalized
        executable = ''
        executableExists = $false
        executableSha256 = ''
        playerLog = $playerLog
        minimumSeconds = $MinimumSmokeSeconds
        observedSeconds = 0
        resolution = '1280x800 windowed'
        windowStyle = 'Hidden'
        processId = 0
        aliveAtMinimum = $false
        respondingAtMinimum = $false
        responseGraceSeconds = 6
        respondingByGraceDeadline = $false
        earlyExit = $false
        exitCode = $null
        terminatedByRunner = $false
        cleanupSucceeded = $false
        residualPackageProcessIds = @()
        developmentPlaytestLog = ''
        developmentPlaytestLogCaptured = $false
        capturedPlaytestLog = $capturedPlaytestLog
        launchError = 'SKIPPED: package manifest or ZIP/folder integrity did not pass.'
        status = 'FAIL'
    }
}
$smokeReport = [ordered]@{
    schemaVersion = 1
    runId = $RunId
    baselineCommit = $baselineCommitNormalized
    packageSourceCommit = $packageSourceCommitNormalized
    observedUtc = [DateTime]::UtcNow.ToString('O')
    smoke = $smoke
    overall = $smoke.status
}
Write-Utf8NoBom (Join-Path $evidenceRoot 'gamejam-package-extracted-hidden-smoke.json') (($smokeReport | ConvertTo-Json -Depth 12) + [Environment]::NewLine)
Write-Utf8Lines (Join-Path $evidenceRoot 'gamejam-package-extracted-hidden-smoke.txt') @(
    'Game Jam extracted package hidden smoke'
    "Run ID: $RunId"
    "QA baseline: $baselineCommitNormalized"
    "Package source commit: $packageSourceCommitNormalized"
    "Extracted root: $extractedPackageRoot"
    "Executable: $($smoke.executable)"
    "Executable SHA-256: $($smoke.executableSha256)"
    "Window style: $($smoke.windowStyle)"
    "Required/observed seconds: $($smoke.minimumSeconds)/$($smoke.observedSeconds)"
    "Alive/responding: $($smoke.aliveAtMinimum)/$($smoke.respondingByGraceDeadline)"
    "Early exit: $($smoke.earlyExit)"
    "Process cleanup / residual PIDs: $($smoke.cleanupSucceeded) / $([string]::Join(',', @($smoke.residualPackageProcessIds)))"
    "Development LocalLow JSONL: $($smoke.developmentPlaytestLog)"
    "Captured evidence JSONL: $($smoke.capturedPlaytestLog) / $($smoke.developmentPlaytestLogCaptured)"
    "Launch error: $($smoke.launchError)"
    "Result: $($smoke.status)"
)

$folderAfter = Get-DirectorySnapshot $packageFolderPath
$zipSha256After = Get-Sha256 $packageZipPath
$sourceImmutable = $folderBefore.aggregateSha256 -eq $folderAfter.aggregateSha256 -and
    $folderBefore.fileCount -eq $folderAfter.fileCount -and $folderBefore.bytes -eq $folderAfter.bytes -and
    $zipSha256Before -eq $zipSha256After
$postSmokeComparison = [pscustomobject][ordered]@{
    expectedFileCount = $folderBefore.fileCount
    actualFileCount = 0
    expectedAggregateSha256 = $folderBefore.aggregateSha256
    actualAggregateSha256 = ''
    missingFromExtracted = @()
    extraInExtracted = @()
    contentMismatches = @()
    status = 'FAIL'
}
if (-not [string]::IsNullOrWhiteSpace($extractedPackageRoot) -and
    (Test-Path -LiteralPath $extractedPackageRoot -PathType Container)) {
    try {
        $extractedAfterSmoke = Get-DirectorySnapshot $extractedPackageRoot
        $postSmokeComparison = Compare-DirectorySnapshots $folderBefore $extractedAfterSmoke
    } catch {
        $postSmokeComparison = [pscustomobject][ordered]@{
            expectedFileCount = $folderBefore.fileCount
            actualFileCount = 0
            expectedAggregateSha256 = $folderBefore.aggregateSha256
            actualAggregateSha256 = ''
            missingFromExtracted = @()
            extraInExtracted = @()
            contentMismatches = @([pscustomobject][ordered]@{ path = '<snapshot>'; error = $_.Exception.Message })
            status = 'FAIL'
        }
    }
}
$testedPayloadImmutable = $postSmokeComparison.status -eq 'PASS' -and
    $smoke.executableSha256 -eq $folderManifestAudit.buildInfo.executableSha256
$checks = @(
    [ordered]@{ id = 'PKG-I01.folder-internal-sha256-manifest'; status = $folderManifestAudit.status },
    [ordered]@{ id = 'PKG-I02.zip-folder-exact-content'; status = $zipComparisonOverall },
    [ordered]@{ id = 'PKG-I03.extracted-hidden-smoke'; status = $smoke.status },
    [ordered]@{ id = 'PKG-I04.source-package-immutable'; status = if ($sourceImmutable) { 'PASS' } else { 'FAIL' } },
    [ordered]@{ id = 'PKG-I05.tested-extracted-payload-immutable'; status = if ($testedPayloadImmutable) { 'PASS' } else { 'FAIL' } },
    [ordered]@{ id = 'PKG-I06.development-playtest-log-captured'; status = if ($smoke.developmentPlaytestLogCaptured) { 'PASS' } else { 'FAIL' } }
)
$overall = if (@($checks | Where-Object { $_.status -ne 'PASS' }).Count -eq 0) { 'PASS' } else { 'FAIL' }
$aggregateExitCode = if ($overall -eq 'PASS') { 0 } else { 1 }
$summaryPath = Join-Path $evidenceRoot 'gamejam-package-integrity-summary.json'
$summaryTextPath = Join-Path $evidenceRoot 'gamejam-package-integrity-summary.txt'
$summary = [ordered]@{
    schemaVersion = 1
    title = 'Game Jam final package integrity and extracted smoke gate'
    runId = $RunId
    baselineCommit = $baselineCommitNormalized
    packageSourceCommit = $packageSourceCommitNormalized
    observedHead = $head
    startedUtc = $startedUtc.ToString('O')
    completedUtc = [DateTime]::UtcNow.ToString('O')
    overall = $overall
    packageFolder = $packageFolderPath
    packageZip = $packageZipPath
    packageZipSha256 = $zipSha256Before
    sourcePackageImmutable = $sourceImmutable
    testedExtractedPayloadImmutable = $testedPayloadImmutable
    postSmokeComparison = $postSmokeComparison
    exitCode = $aggregateExitCode
    outputPolicy = 'The runner writes repo files only below fresh Artifacts/ParallelQA/<RunId> and work/ParallelQA/<RunId>. The Development player also creates one timestamped JSONL below its normal LocalLow PlaytestLogs path; that path is disclosed and a copy is captured as evidence without deleting user data.'
    evidenceRoot = $evidenceRoot
    workRoot = $workRoot
    checks = $checks
    reports = [ordered]@{
        folderManifestJson = Join-Path $evidenceRoot 'gamejam-package-folder-manifest.json'
        folderManifestText = Join-Path $evidenceRoot 'gamejam-package-folder-manifest.txt'
        zipFolderComparisonJson = Join-Path $evidenceRoot 'gamejam-package-zip-folder-comparison.json'
        zipFolderComparisonText = Join-Path $evidenceRoot 'gamejam-package-zip-folder-comparison.txt'
        extractedSmokeJson = Join-Path $evidenceRoot 'gamejam-package-extracted-hidden-smoke.json'
        extractedSmokeText = Join-Path $evidenceRoot 'gamejam-package-extracted-hidden-smoke.txt'
        capturedDevelopmentPlaytestLog = $capturedPlaytestLog
    }
    exactRerun = "& '.\Assets\Editor\ParallelQA\Invoke-GameJamPackageIntegrityGate.ps1' -RunId '<FRESH_RUN_ID>' -BaselineCommit '$baselineCommitNormalized' -PackageSourceCommit '$packageSourceCommitNormalized' -PackageFolder '$packageFolderPath' -PackageZip '$packageZipPath' -MinimumSmokeSeconds $MinimumSmokeSeconds"
}
Write-Utf8NoBom $summaryPath (($summary | ConvertTo-Json -Depth 12) + [Environment]::NewLine)
Write-Utf8Lines $summaryTextPath @(
    'Game Jam final package integrity and extracted smoke gate'
    "Run ID: $RunId"
    "QA baseline / observed HEAD: $baselineCommitNormalized / $head"
    "Package source commit: $packageSourceCommitNormalized"
    "Result: $overall"
    "Exit code: $aggregateExitCode"
    "Folder manifest: $($folderManifestAudit.status)"
    "ZIP versus folder: $zipComparisonOverall"
    "Extracted hidden smoke: $($smoke.status)"
    "Source package unchanged: $sourceImmutable"
    "Tested extracted payload unchanged: $testedPayloadImmutable"
    "Development LocalLow JSONL disclosed/captured: $($smoke.developmentPlaytestLog) / $($smoke.developmentPlaytestLogCaptured)"
    "ZIP SHA-256: $zipSha256Before"
    "Evidence: $evidenceRoot"
    "Work: $workRoot"
)

Write-Output "PACKAGE_INTEGRITY=$overall"
Write-Output "FOLDER_MANIFEST=$($folderManifestAudit.status)"
Write-Output "ZIP_FOLDER_COMPARISON=$zipComparisonOverall"
Write-Output "EXTRACTED_HIDDEN_SMOKE=$($smoke.status)"
Write-Output "SOURCE_PACKAGE_IMMUTABLE=$sourceImmutable"
Write-Output "TESTED_EXTRACTED_PAYLOAD_IMMUTABLE=$testedPayloadImmutable"
Write-Output "EVIDENCE=$evidenceRoot"
exit $aggregateExitCode
