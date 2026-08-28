param(
    [Parameter(Mandatory = $true)]
    [string]$SourceSheet,
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$regionNames = @(
    'beach',
    'shallows',
    'forest',
    'ridge-highland',
    'island-cave',
    'wreck-cove',
    'ruins-relay'
)

$sourcePath = (Resolve-Path -LiteralPath $SourceSheet).Path
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$outputPath = (Resolve-Path -LiteralPath $OutputDirectory).Path
$source = [System.Drawing.Bitmap]::FromFile($sourcePath)

try {
    $dividerRows = New-Object 'System.Collections.Generic.List[int]'
    for ($y = 0; $y -lt $source.Height; $y += 1) {
        $white = 0
        for ($x = 0; $x -lt $source.Width; $x += 4) {
            $pixel = $source.GetPixel($x, $y)
            if ($pixel.R -ge 242 -and $pixel.G -ge 242 -and $pixel.B -ge 242) {
                $white += 1
            }
        }
        # ImageGen anti-aliases some divider pixels. A divider is still globally
        # horizontal, so an 82% near-white coverage threshold is deterministic
        # and safely distinct from clouds or surf inside a panel.
        if ($white -ge [Math]::Floor(($source.Width / 4) * 0.82)) {
            $dividerRows.Add($y)
        }
    }

    $dividerBands = New-Object 'System.Collections.Generic.List[object]'
    foreach ($row in $dividerRows) {
        if ($dividerBands.Count -eq 0 -or $row -gt ($dividerBands[$dividerBands.Count - 1].End + 1)) {
            $dividerBands.Add([pscustomobject]@{ Start = $row; End = $row })
        }
        else {
            $dividerBands[$dividerBands.Count - 1].End = $row
        }
    }

    $segments = New-Object 'System.Collections.Generic.List[object]'
    $start = 0
    foreach ($band in $dividerBands) {
        $height = $band.Start - $start
        if ($height -ge 64) {
            $segments.Add([pscustomobject]@{ Start = $start; Height = $height })
        }
        $start = $band.End + 1
    }
    if (($source.Height - $start) -ge 64) {
        $segments.Add([pscustomobject]@{ Start = $start; Height = $source.Height - $start })
    }

    if ($segments.Count -ne $regionNames.Count) {
        throw "Expected $($regionNames.Count) region panels but detected $($segments.Count)."
    }

    $manifestRegions = New-Object 'System.Collections.Generic.List[object]'
    for ($index = 0; $index -lt $segments.Count; $index += 1) {
        $segment = $segments[$index]
        $name = $regionNames[$index]
        $rect = New-Object System.Drawing.Rectangle(0, $segment.Start, $source.Width, $segment.Height)
        $background = $source.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $foreground = New-Object System.Drawing.Bitmap($source.Width, $segment.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $foregroundStart = [Math]::Floor($segment.Height * 0.66)
            for ($localY = $foregroundStart; $localY -lt $segment.Height; $localY += 1) {
                for ($x = 0; $x -lt $source.Width; $x += 1) {
                    $pixel = $background.GetPixel($x, $localY)
                    $luma = 0.2126 * $pixel.R + 0.7152 * $pixel.G + 0.0722 * $pixel.B
                    $max = [Math]::Max($pixel.R, [Math]::Max($pixel.G, $pixel.B))
                    $min = [Math]::Min($pixel.R, [Math]::Min($pixel.G, $pixel.B))
                    $saturation = if ($max -eq 0) { 0.0 } else { ($max - $min) / [double]$max }
                    $opacity = [Math]::Max(0.0, [Math]::Min(1.0, (142.0 - $luma) / 72.0))
                    if ($saturation -gt 0.35 -and $luma -lt 170.0) {
                        $opacity = [Math]::Max($opacity, [Math]::Min(0.78, (170.0 - $luma) / 95.0))
                    }
                    $alpha = [int][Math]::Round($opacity * 210.0)
                    if ($alpha -gt 4) {
                        $foreground.SetPixel($x, $localY, [System.Drawing.Color]::FromArgb($alpha, $pixel.R, $pixel.G, $pixel.B))
                    }
                }
            }

            $backgroundFile = Join-Path $outputPath ("o11-region-{0}-background.png" -f $name)
            $foregroundFile = Join-Path $outputPath ("o11-region-{0}-foreground.png" -f $name)
            $background.Save($backgroundFile, [System.Drawing.Imaging.ImageFormat]::Png)
            $foreground.Save($foregroundFile, [System.Drawing.Imaging.ImageFormat]::Png)
            $manifestRegions.Add([ordered]@{
                id = "region.$name"
                sourceRect = @($rect.X, $rect.Y, $rect.Width, $rect.Height)
                background = [System.IO.Path]::GetFileName($backgroundFile)
                foreground = [System.IO.Path]::GetFileName($foregroundFile)
                pivot = @(0.5, 0.0)
                pixelsPerUnit = 100
                backgroundSortingOrder = -20
                foregroundSortingOrder = 1
            })
        }
        finally {
            $background.Dispose()
            $foreground.Dispose()
        }
    }

    $manifest = [ordered]@{
        schemaVersion = 1
        source = [System.IO.Path]::GetFileName($sourcePath)
        sourceSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourcePath).Hash.ToLowerInvariant()
        processing = 'deterministic horizontal-divider slice plus dark/saturated lower-band alpha extraction'
        retryCount = 0
        reviewState = 'review'
        runtimeUse = 'provisional O11 production presentation requested by user; no adoption implication'
        regions = $manifestRegions
    }
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outputPath 'o11-region-runtime-manifest.json') -Encoding utf8
}
finally {
    $source.Dispose()
}
