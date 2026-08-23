param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$wave11Root = Join-Path $projectRoot 'Assets/_Project/Art/Generated/ui_set/job_20260823094919_b97f9f61'
$releaseRoot = Join-Path $projectRoot 'Artifacts/ParallelQA/20260823T125000Z_13ecded_release'

$comparisonPath = Join-Path $wave11Root 'wave11-slot-affordance-comparison-board.png'
$completeBasePath = Join-Path $projectRoot 'Assets/_Project/Art/Generated/background/job_20260823042510_f2a3ec9e/recomposed-spatial-camp.png'

$rows = @(
    [pscustomobject]@{
        Id = 'slot.start.upper'
        Direction = 'UPPER / UP'
        PlayerRect = @(309.3, 411.3, 85.4, 152.6)
        TargetRect = @(316.4, 503.1, 78.2, 103.1)
        ConnectorRect = @(570.0, 264.0, 104.0, 26.0)
        Available = Join-Path $releaseRoot 'wave11-slot-upper-ko-approach-1280x800.png'
        Selected = Join-Path $releaseRoot 'kim-survival-wave11-module-upper-ko-1280x800.png'
        Insufficient = Join-Path $releaseRoot 'wave11-slot-upper-ko-preview-1280x800.png'
    },
    [pscustomobject]@{
        Id = 'slot.start.side'
        Direction = 'SIDE / RIGHT'
        PlayerRect = @(1169.8, 411.3, 85.4, 152.6)
        TargetRect = @(1176.9, 503.1, 78.2, 103.1)
        ConnectorRect = @(763.0, 453.0, 24.0, 104.0)
        Available = Join-Path $releaseRoot 'wave11-slot-side-en-approach-1280x800.png'
        Selected = Join-Path $releaseRoot 'kim-survival-wave11-module-side-en-1280x800.png'
        Insufficient = Join-Path $releaseRoot 'wave11-slot-side-en-preview-1280x800.png'
    },
    [pscustomobject]@{
        Id = 'slot.start.basement'
        Direction = 'BASEMENT / DOWN'
        PlayerRect = @(700.4, 411.3, 85.4, 152.6)
        TargetRect = @(707.6, 503.1, 78.2, 103.1)
        ConnectorRect = @(391.0, 496.0, 104.0, 26.0)
        Available = Join-Path $releaseRoot 'wave11-slot-basement-qps-long-approach-1280x800.png'
        Selected = Join-Path $releaseRoot 'kim-survival-wave11-module-basement-qps-long-1280x800.png'
        Insufficient = Join-Path $releaseRoot 'wave11-slot-basement-qps-long-preview-1280x800.png'
    }
)

$required = @($comparisonPath, $completeBasePath)
foreach ($row in $rows) {
    $required += @($row.Available, $row.Selected, $row.Insufficient)
}
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing source: $path"
    }
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$canvasWidth = 3840
$canvasHeight = 2800
$tileWidth = 800
$tileHeight = 500
$tileScale = 0.625
$tileXs = @(420, 1260, 2100, 2940)
$tileYs = @(1130, 1650, 2170)

$canvas = New-Object System.Drawing.Bitmap($canvasWidth, $canvasHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.Clear([System.Drawing.Color]::Transparent)

$backgroundBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 14, 34, 43))
$panelBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 25, 57, 68))
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 245, 228, 175))
$mutedBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 178, 207, 207))
$tealPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 65, 208, 202), 4)
$amberPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 249, 190, 65), 4)
$redPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 255, 91, 91), 4)
$mintPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 91, 224, 179), 4)
$safeNarrationPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 255, 229, 163), 3)
$safePromptPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 65, 208, 202), 3)
$safePlayerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 255, 255, 255), 3)
$safeTargetPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 249, 190, 65), 3)
$safeWalkPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(210, 91, 224, 179), 3)

$titleFont = New-Object System.Drawing.Font('Segoe UI', 34, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$subtitleFont = New-Object System.Drawing.Font('Segoe UI', 20, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$headerFont = New-Object System.Drawing.Font('Segoe UI', 24, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$rowFont = New-Object System.Drawing.Font('Segoe UI', 22, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$smallFont = New-Object System.Drawing.Font('Segoe UI', 17, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)

try {
    $graphics.FillRectangle($backgroundBrush, 1, 1, $canvasWidth - 2, $canvasHeight - 2)
    $graphics.DrawString('WAVE 13 - MODULE CONNECTOR REVIEW-ONLY HANDOFF', $titleFont, $textBrush, 42, 24)
    $graphics.DrawString('All three Wave 11 candidates remain unselected. Runtime allowlist is empty.', $subtitleFont, $mutedBrush, 44, 72)

    $comparison = [System.Drawing.Image]::FromFile($comparisonPath)
    try {
        $graphics.DrawImage($comparison, (New-Object System.Drawing.Rectangle(120, 115, 3600, 900)))
    }
    finally {
        $comparison.Dispose()
    }

    $columnNames = @('AVAILABLE / NEAR', 'SELECTED / PREVIEW', 'COST SHORT', 'BUILD COMPLETE')
    $columnPens = @($tealPen, $amberPen, $redPen, $mintPen)
    for ($column = 0; $column -lt 4; $column++) {
        $graphics.DrawString($columnNames[$column], $headerFont, $textBrush, $tileXs[$column] + 12, 1070)
    }

    for ($rowIndex = 0; $rowIndex -lt $rows.Count; $rowIndex++) {
        $row = $rows[$rowIndex]
        $tileY = $tileYs[$rowIndex]
        $graphics.FillRectangle($panelBrush, 24, $tileY, 366, $tileHeight)
        $graphics.DrawString($row.Direction, $rowFont, $textBrush, 48, $tileY + 34)
        $graphics.DrawString($row.Id, $smallFont, $mutedBrush, 48, $tileY + 76)
        $graphics.DrawString('1280x800 source', $smallFont, $mutedBrush, 48, $tileY + 112)
        $graphics.DrawString('N narration', $smallFont, $mutedBrush, 48, $tileY + 170)
        $graphics.DrawString('P compact prompt', $smallFont, $mutedBrush, 48, $tileY + 202)
        $graphics.DrawString('K player', $smallFont, $mutedBrush, 48, $tileY + 234)
        $graphics.DrawString('T target', $smallFont, $mutedBrush, 48, $tileY + 266)
        $graphics.DrawString('W walking band', $smallFont, $mutedBrush, 48, $tileY + 298)

        $paths = @($row.Available, $row.Selected, $row.Insufficient, $completeBasePath)
        for ($column = 0; $column -lt 4; $column++) {
            $tileX = $tileXs[$column]
            $image = [System.Drawing.Image]::FromFile($paths[$column])
            try {
                $graphics.DrawImage($image, (New-Object System.Drawing.Rectangle($tileX, $tileY, $tileWidth, $tileHeight)))
            }
            finally {
                $image.Dispose()
            }

            $graphics.DrawRectangle($columnPens[$column], $tileX, $tileY, $tileWidth - 1, $tileHeight - 1)

            if ($column -lt 3) {
                $narration = @(230.4, 144.0, 819.2, 136.0)
                $prompt = @(420.0, 292.0, 440.0, 44.0)
                $walking = @(0.0, 535.1, 1280.0, 81.8)
                $rectSets = @(
                    @($narration, $safeNarrationPen),
                    @($prompt, $safePromptPen),
                    @($row.PlayerRect, $safePlayerPen),
                    @($row.TargetRect, $safeTargetPen),
                    @($walking, $safeWalkPen)
                )
                foreach ($entry in $rectSets) {
                    $rect = $entry[0]
                    $pen = $entry[1]
                    $drawRect = New-Object System.Drawing.RectangleF(
                        [single]($tileX + ($rect[0] * $tileScale)),
                        [single]($tileY + ($rect[1] * $tileScale)),
                        [single]($rect[2] * $tileScale),
                        [single]($rect[3] * $tileScale)
                    )
                    $graphics.DrawRectangle($pen, $drawRect.X, $drawRect.Y, $drawRect.Width, $drawRect.Height)
                }
            }
            else {
                $connector = $row.ConnectorRect
                $connectorX = [single]($tileX + ($connector[0] * $tileScale))
                $connectorY = [single]($tileY + ($connector[1] * $tileScale))
                $connectorWidth = [single]($connector[2] * $tileScale)
                $connectorHeight = [single]($connector[3] * $tileScale)
                $graphics.DrawRectangle($mintPen, $connectorX, $connectorY, $connectorWidth, $connectorHeight)
                $checkX = $connectorX + $connectorWidth + 12
                $checkY = $connectorY + [Math]::Max(8, $connectorHeight * 0.25)
                $graphics.DrawLine($mintPen, $checkX, $checkY + 10, $checkX + 8, $checkY + 18)
                $graphics.DrawLine($mintPen, $checkX + 8, $checkY + 18, $checkX + 24, $checkY)
                $graphics.DrawLine($mintPen, $checkX + 9, $checkY + 10, $checkX + 17, $checkY + 18)
                $graphics.DrawLine($mintPen, $checkX + 17, $checkY + 18, $checkX + 33, $checkY)
                $graphics.DrawString('Review composite: built room + permanent connector; slot marker retired.', $smallFont, $textBrush, $tileX + 18, $tileY + 452)
            }
        }
    }

    $graphics.DrawString('SAFE-ZONE: narration TL(230.4,144,819.2,136) -> 12px -> prompt TL(420,292,440,44); world overlays stay inside physical connector footprints.', $smallFont, $mutedBrush, 40, 2718)
    $graphics.DrawString('REVIEW EVIDENCE ONLY - no candidate adopted, packaged, Addressable, Resource-loaded, or scene-connected.', $smallFont, $textBrush, 40, 2750)

    $canvas.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $titleFont.Dispose()
    $subtitleFont.Dispose()
    $headerFont.Dispose()
    $rowFont.Dispose()
    $smallFont.Dispose()
    $backgroundBrush.Dispose()
    $panelBrush.Dispose()
    $textBrush.Dispose()
    $mutedBrush.Dispose()
    $tealPen.Dispose()
    $amberPen.Dispose()
    $redPen.Dispose()
    $mintPen.Dispose()
    $safeNarrationPen.Dispose()
    $safePromptPen.Dispose()
    $safePlayerPen.Dispose()
    $safeTargetPen.Dispose()
    $safeWalkPen.Dispose()
    $graphics.Dispose()
    $canvas.Dispose()
}

Write-Output $OutputPath
