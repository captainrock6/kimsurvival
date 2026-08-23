param(
    [Parameter(Mandatory = $true)]
    [string]$CleanBackgroundPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$originalPath = Join-Path $projectRoot 'Assets/_Project/Art/Generated/background/job_20260822095339_18288994/exec-d8f22840-a9a6-42bd-8b43-6f4f7dff569f.png'
$barrierRoot = Join-Path $projectRoot 'Assets/_Project/Art/Generated/separated_parts/job_20260822234631_ac651d92'
$blockedPath = Join-Path $barrierRoot 'blocked.png'
$clearedPath = Join-Path $barrierRoot 'cleared.png'

foreach ($path in @($CleanBackgroundPath, $originalPath, $blockedPath, $clearedPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing Selected B source: $path"
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

function New-OpaqueCanvas([int]$Width, [int]$Height, [System.Drawing.Color]$Color) {
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear($Color)
    $graphics.Dispose()
    return $bitmap
}

function New-Graphics([System.Drawing.Bitmap]$Bitmap) {
    $graphics = [System.Drawing.Graphics]::FromImage($Bitmap)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    return $graphics
}

function Draw-KimProxy([System.Drawing.Graphics]$Graphics, [int]$X, [int]$GroundY) {
    $outline = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 24, 32, 31), 5)
    $skin = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 230, 164, 107))
    $shirt = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 227, 100, 39))
    $shorts = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 71, 94, 56))
    try {
        $Graphics.FillEllipse($skin, $X + 20, $GroundY - 146, 40, 40)
        $Graphics.DrawEllipse($outline, $X + 20, $GroundY - 146, 40, 40)
        $Graphics.FillRectangle($shirt, $X + 17, $GroundY - 106, 49, 62)
        $Graphics.DrawRectangle($outline, $X + 17, $GroundY - 106, 49, 62)
        $Graphics.FillRectangle($shorts, $X + 20, $GroundY - 44, 44, 28)
        $Graphics.DrawRectangle($outline, $X + 20, $GroundY - 44, 44, 28)
        $Graphics.DrawLine($outline, $X + 30, $GroundY - 16, $X + 25, $GroundY)
        $Graphics.DrawLine($outline, $X + 55, $GroundY - 16, $X + 62, $GroundY)
    }
    finally {
        $outline.Dispose()
        $skin.Dispose()
        $shirt.Dispose()
        $shorts.Dispose()
    }
}

function New-ActualReview([string]$BarrierPath, [string]$OutputPath) {
    $bitmap = New-OpaqueCanvas 1280 800 ([System.Drawing.Color]::FromArgb(255, 12, 31, 40))
    $graphics = New-Graphics $bitmap
    $background = [System.Drawing.Image]::FromFile($CleanBackgroundPath)
    $barrier = [System.Drawing.Image]::FromFile($BarrierPath)
    $hudBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(220, 13, 35, 46))
    $safeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(36, 247, 229, 164))
    $safePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 247, 229, 164), 3)
    $promptPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(225, 58, 207, 198), 3)
    $labelBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 251, 238, 194))
    $font = New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    try {
        $graphics.DrawImage($background, (New-Object System.Drawing.Rectangle(0, 40, 1280, 720)))
        $graphics.DrawImage($barrier, (New-Object System.Drawing.Rectangle(735, 415, 205, 205)))
        Draw-KimProxy $graphics 603 625

        $graphics.FillRectangle($hudBrush, 16, 14, 1247, 119)
        $graphics.FillRectangle($hudBrush, 16, 685, 1247, 101)
        $graphics.FillRectangle($safeBrush, 230, 144, 819, 136)
        $graphics.DrawRectangle($safePen, 230, 144, 819, 136)
        $graphics.DrawRectangle($promptPen, 420, 292, 440, 44)
        $graphics.DrawString('REVIEW ONLY - SELECTED B PRODUCTION DERIVATIVE', $font, $labelBrush, 36, 38)
        $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $background.Dispose()
        $barrier.Dispose()
        $hudBrush.Dispose()
        $safeBrush.Dispose()
        $safePen.Dispose()
        $promptPen.Dispose()
        $labelBrush.Dispose()
        $font.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$cleanImage = [System.Drawing.Image]::FromFile($CleanBackgroundPath)
try {
    $width = $cleanImage.Width
    $height = $cleanImage.Height
}
finally {
    $cleanImage.Dispose()
}

$gameplayMaskPath = Join-Path $OutputDirectory 'gameplay-band-mask.png'
$gameplayMask = New-OpaqueCanvas $width $height ([System.Drawing.Color]::Black)
$graphics = New-Graphics $gameplayMask
$bandBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Rectangle(0, 300, $width, 500)),
    [System.Drawing.Color]::Black,
    [System.Drawing.Color]::Black,
    90
)
$blend = New-Object System.Drawing.Drawing2D.ColorBlend
$blend.Positions = [single[]]@(0.0, 0.18, 0.38, 0.78, 1.0)
$blend.Colors = [System.Drawing.Color[]]@(
    [System.Drawing.Color]::FromArgb(255, 0, 0, 0),
    [System.Drawing.Color]::FromArgb(255, 48, 48, 48),
    [System.Drawing.Color]::FromArgb(255, 255, 255, 255),
    [System.Drawing.Color]::FromArgb(255, 255, 255, 255),
    [System.Drawing.Color]::FromArgb(255, 0, 0, 0)
)
$bandBrush.InterpolationColors = $blend
try {
    $graphics.FillRectangle($bandBrush, 0, 300, $width, 500)
    $gameplayMask.Save($gameplayMaskPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $bandBrush.Dispose()
    $graphics.Dispose()
    $gameplayMask.Dispose()
}

$depthMaskPath = Join-Path $OutputDirectory 'depth-subdue-mask.png'
$depthMask = New-OpaqueCanvas $width $height ([System.Drawing.Color]::White)
$graphics = New-Graphics $depthMask
$laneBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 0, 0))
$edgeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 192, 192, 192))
try {
    $graphics.FillEllipse($laneBrush, [int]($width * 0.08), [int]($height * 0.42), [int]($width * 0.82), [int]($height * 0.34))
    $graphics.FillRectangle($edgeBrush, 0, [int]($height * 0.84), $width, [int]($height * 0.16))
    $depthMask.Save($depthMaskPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $laneBrush.Dispose()
    $edgeBrush.Dispose()
    $graphics.Dispose()
    $depthMask.Dispose()
}

$blockedReviewPath = Join-Path $OutputDirectory 'selected-b-blocked-review-1280x800.png'
$clearedReviewPath = Join-Path $OutputDirectory 'selected-b-cleared-review-1280x800.png'
New-ActualReview $blockedPath $blockedReviewPath
New-ActualReview $clearedPath $clearedReviewPath

$boardPath = Join-Path $OutputDirectory 'selected-b-production-review-board.png'
$board = New-OpaqueCanvas 3840 2550 ([System.Drawing.Color]::FromArgb(255, 12, 31, 40))
$graphics = New-Graphics $board
$titleFont = New-Object System.Drawing.Font('Segoe UI', 38, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$headerFont = New-Object System.Drawing.Font('Segoe UI', 25, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$bodyFont = New-Object System.Drawing.Font('Segoe UI', 19, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 249, 233, 187))
$mutedBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 178, 205, 205))
$panelBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 24, 54, 63))
$tealPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 72, 214, 185), 4)
try {
    $graphics.DrawString('SELECTED B - CLEAN PRODUCTION DERIVATIVE / REVIEW ONLY', $titleFont, $textBrush, 54, 28)
    $graphics.DrawString('Direction adopted; this newly edited bitmap still requires exact-art approval before packaging or runtime connection.', $bodyFont, $mutedBrush, 58, 82)

    $graphics.FillRectangle($panelBrush, 40, 130, 1850, 1080)
    $graphics.FillRectangle($panelBrush, 1950, 130, 1850, 1080)
    $graphics.DrawString('BEFORE - BAKED OBSTRUCTION', $headerFont, $textBrush, 62, 150)
    $graphics.DrawString('AFTER - CLEAN FOREST PASSAGE', $headerFont, $textBrush, 1972, 150)
    $before = [System.Drawing.Image]::FromFile($originalPath)
    $after = [System.Drawing.Image]::FromFile($CleanBackgroundPath)
    try {
        $graphics.DrawImage($before, (New-Object System.Drawing.Rectangle(60, 200, 1810, 1019)))
        $graphics.DrawImage($after, (New-Object System.Drawing.Rectangle(1970, 200, 1810, 1019)))
    }
    finally {
        $before.Dispose()
        $after.Dispose()
    }

    $graphics.FillRectangle($panelBrush, 40, 1260, 1850, 1210)
    $graphics.FillRectangle($panelBrush, 1950, 1260, 1850, 1210)
    $graphics.DrawString('BLOCKED STATE COMPOSITE - 1280x800', $headerFont, $textBrush, 62, 1280)
    $graphics.DrawString('CLEARED STATE COMPOSITE - 1280x800', $headerFont, $textBrush, 1972, 1280)
    $blocked = [System.Drawing.Image]::FromFile($blockedReviewPath)
    $cleared = [System.Drawing.Image]::FromFile($clearedReviewPath)
    try {
        $graphics.DrawImage($blocked, (New-Object System.Drawing.Rectangle(75, 1340, 1780, 1113)))
        $graphics.DrawImage($cleared, (New-Object System.Drawing.Rectangle(1985, 1340, 1780, 1113)))
    }
    finally {
        $blocked.Dispose()
        $cleared.Dispose()
    }
    $graphics.DrawRectangle($tealPen, 75, 1340, 1779, 1112)
    $graphics.DrawRectangle($tealPen, 1985, 1340, 1779, 1112)
    $graphics.DrawString('Review composites only: Kim proxy and HUD-safe overlays are not production sprites. Masks stay independent and are disabled until runtime integration is authorized.', $bodyFont, $mutedBrush, 70, 2492)
    $board.Save($boardPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $titleFont.Dispose()
    $headerFont.Dispose()
    $bodyFont.Dispose()
    $textBrush.Dispose()
    $mutedBrush.Dispose()
    $panelBrush.Dispose()
    $tealPen.Dispose()
    $graphics.Dispose()
    $board.Dispose()
}

Get-ChildItem -LiteralPath $OutputDirectory -File | Select-Object -ExpandProperty FullName
