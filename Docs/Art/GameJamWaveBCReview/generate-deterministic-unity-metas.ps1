param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path,
    [string]$JobRelativePath = 'Assets/_Project/Art/Generated/ui_set/job_20260825171815_f7640e25'
)

$ErrorActionPreference = 'Stop'

function Get-DeterministicGuid([string]$AssetRelativePath) {
    $normalized = $AssetRelativePath.Replace('\', '/').ToLowerInvariant()
    $seed = "kimsurvival-unity-meta-v1|$normalized"
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($seed)
        $hash = $sha.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hash).Replace('-', '').ToLowerInvariant()).Substring(0, 32)
    }
    finally {
        $sha.Dispose()
    }
}

function Get-RelativeAssetPath([string]$AbsolutePath) {
    return [System.IO.Path]::GetRelativePath($ProjectRoot, $AbsolutePath).Replace('\', '/')
}

function Read-Template([string]$RelativePath, [bool]$PreserveTrailingWhitespace = $false) {
    $path = Join-Path $ProjectRoot $RelativePath
    if (!(Test-Path -LiteralPath $path)) {
        throw "Unity meta template is missing: $RelativePath"
    }
    $content = Get-Content -Raw -LiteralPath $path
    if ($PreserveTrailingWhitespace) {
        return $content
    }
    return [regex]::Replace($content, '(?m)[ \t]+(?=\r?$)', '')
}

function Set-MetaGuid([string]$Template, [string]$Guid) {
    $result = [regex]::Replace($Template, '(?m)^guid: [0-9a-f]{32}\r?$', "guid: $Guid", 1)
    if ($result -eq $Template) {
        throw 'Template GUID replacement failed.'
    }
    return $result
}

$jobPath = (Resolve-Path -LiteralPath (Join-Path $ProjectRoot $JobRelativePath)).Path
$resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
if (!$jobPath.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Job path is outside the project root.'
}

$templates = @{
    folder = Read-Template 'Assets/_Project/Art/Generated/ui_set/job_20260823145302_c4c41491.meta'
    texture = Read-Template 'Assets/_Project/Art/Generated/ui_set/job_20260823145302_c4c41491/expedition-icons-atlas.png.meta'
    svg = Read-Template 'Assets/_Project/Art/Generated/ui_set/job_20260823145302_c4c41491/expedition-icons-atlas.svg.meta' $true
    text = Read-Template 'Assets/_Project/Art/Generated/ui_set/job_20260823145302_c4c41491/job.json.meta'
}

$targets = @(
    [pscustomobject]@{
        AssetPath = $jobPath
        RelativePath = Get-RelativeAssetPath $jobPath
        MetaPath = "$jobPath.meta"
        Template = $templates.folder
        Kind = 'folder'
    }
)

Get-ChildItem -LiteralPath $jobPath -File |
    Where-Object { $_.Extension -ne '.meta' } |
    Sort-Object Name |
    ForEach-Object {
        $kind = switch ($_.Extension.ToLowerInvariant()) {
            '.png' { 'texture' }
            '.jpg' { 'texture' }
            '.jpeg' { 'texture' }
            '.svg' { 'svg' }
            '.json' { 'text' }
            '.txt' { 'text' }
            '.html' { 'text' }
            default { throw "Unsupported Unity asset extension: $($_.Extension)" }
        }
        $targets += [pscustomobject]@{
            AssetPath = $_.FullName
            RelativePath = Get-RelativeAssetPath $_.FullName
            MetaPath = "$($_.FullName).meta"
            Template = $templates[$kind]
            Kind = $kind
        }
    }

$planned = foreach ($target in $targets) {
    [pscustomobject]@{
        AssetPath = $target.AssetPath
        RelativePath = $target.RelativePath
        MetaPath = $target.MetaPath
        Template = $target.Template
        Kind = $target.Kind
        Guid = Get-DeterministicGuid $target.RelativePath
    }
}

$duplicatePlanned = $planned | Group-Object Guid | Where-Object Count -gt 1
if ($duplicatePlanned) {
    throw "Deterministic GUID collision inside target set: $($duplicatePlanned.Name -join ', ')"
}

$targetMetaPaths = @($planned.MetaPath | ForEach-Object { [System.IO.Path]::GetFullPath($_) })
$existingGuidOwners = @{}
Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets') -Recurse -File -Filter '*.meta' |
    Where-Object { $targetMetaPaths -notcontains $_.FullName } |
    ForEach-Object {
        $match = Select-String -LiteralPath $_.FullName -Pattern '^guid: ([0-9a-f]{32})$'
        if ($match) {
            $guid = $match.Matches[0].Groups[1].Value
            if ($existingGuidOwners.ContainsKey($guid)) {
                throw "Existing Unity GUID is already duplicated: $guid"
            }
            $existingGuidOwners[$guid] = $_.FullName
        }
    }

foreach ($entry in $planned) {
    if ($existingGuidOwners.ContainsKey($entry.Guid)) {
        throw "Deterministic GUID collides with existing asset: $($entry.Guid) / $($existingGuidOwners[$entry.Guid])"
    }
}

$created = 0
$normalized = 0
$unchanged = 0
foreach ($entry in $planned) {
    $content = Set-MetaGuid $entry.Template $entry.Guid
    if (Test-Path -LiteralPath $entry.MetaPath) {
        $match = Select-String -LiteralPath $entry.MetaPath -Pattern '^guid: ([0-9a-f]{32})$'
        if (!$match -or $match.Matches[0].Groups[1].Value -ne $entry.Guid) {
            throw "Existing meta has a non-deterministic GUID: $($entry.MetaPath)"
        }

        $existingContent = Get-Content -Raw -LiteralPath $entry.MetaPath
        if ($existingContent -cne $content) {
            [System.IO.File]::WriteAllText($entry.MetaPath, $content, [System.Text.UTF8Encoding]::new($false))
            $normalized++
            continue
        }

        $unchanged++
        continue
    }

    [System.IO.File]::WriteAllText($entry.MetaPath, $content, [System.Text.UTF8Encoding]::new($false))
    $created++
}

[ordered]@{
    schemaVersion = 1
    algorithm = 'first-128-bits-of-sha256(kimsurvival-unity-meta-v1|lowercase-project-relative-path)'
    jobRelativePath = $JobRelativePath.Replace('\', '/')
    targetCount = $planned.Count
    folderMetaCount = @($planned | Where-Object Kind -eq 'folder').Count
    fileMetaCount = @($planned | Where-Object Kind -ne 'folder').Count
    created = $created
    normalized = $normalized
    unchanged = $unchanged
    entries = @($planned | ForEach-Object { [ordered]@{ path=$_.RelativePath; kind=$_.Kind; guid=$_.Guid } })
} | ConvertTo-Json -Depth 6
