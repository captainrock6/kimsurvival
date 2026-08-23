param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$coastPath = Join-Path $projectRoot 'Assets/_Project/Art/Generated/background/job_20260822095339_18288994/exec-d8f22840-a9a6-42bd-8b43-6f4f7dff569f.png'
$barrierPath = Join-Path $projectRoot 'Assets/_Project/Art/Generated/separated_parts/job_20260822234631_ac651d92/blocked.png'
$iconRoot = Join-Path $projectRoot 'Assets/_Project/Art/Generated/logo_icon/job_20260822141317_caf8e11d'
$explorationPath = Join-Path $projectRoot 'Artifacts/ParallelQA/20260823T131000Z_8eecfa2_integrated/playmode-ko-day2-exploration-1280x800.png'
$swimmingPath = Join-Path $projectRoot 'Artifacts/ParallelQA/20260823T131000Z_8eecfa2_integrated/playmode-ko-day1-swimming-1280x800.png'

$required = @(
    $coastPath,
    $barrierPath,
    (Join-Path $iconRoot 'wood.png'),
    (Join-Path $iconRoot 'stone.png'),
    (Join-Path $iconRoot 'food.png'),
    (Join-Path $iconRoot 'scrap.png'),
    $explorationPath,
    $swimmingPath
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing Wave 14 source: $path"
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

function Draw-KimEnvelope([System.Drawing.Graphics]$Graphics, [int]$X, [int]$GroundY) {
    $outline = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 24, 32, 31), 5)
    $skin = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 230, 164, 107))
    $shirt = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 227, 100, 39))
    $shorts = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 71, 94, 56))
    $bag = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 189, 164, 112))
    try {
        $Graphics.FillEllipse($skin, $X + 22, $GroundY - 150, 42, 42)
        $Graphics.DrawEllipse($outline, $X + 22, $GroundY - 150, 42, 42)
        $Graphics.FillEllipse($bag, $X + 3, $GroundY - 112, 34, 64)
        $Graphics.DrawEllipse($outline, $X + 3, $GroundY - 112, 34, 64)
        $Graphics.FillRectangle($shirt, $X + 20, $GroundY - 111, 50, 64)
        $Graphics.DrawRectangle($outline, $X + 20, $GroundY - 111, 50, 64)
        $Graphics.FillRectangle($shorts, $X + 24, $GroundY - 48, 44, 30)
        $Graphics.DrawRectangle($outline, $X + 24, $GroundY - 48, 44, 30)
        $Graphics.DrawLine($outline, $X + 34, $GroundY - 18, $X + 28, $GroundY)
        $Graphics.DrawLine($outline, $X + 57, $GroundY - 18, $X + 64, $GroundY)
        $Graphics.DrawLine($outline, $X + 69, $GroundY - 98, $X + 86, $GroundY - 65)
    }
    finally {
        $outline.Dispose()
        $skin.Dispose()
        $shirt.Dispose()
        $shorts.Dispose()
        $bag.Dispose()
    }
}

function Draw-Node([System.Drawing.Graphics]$Graphics, [string]$Path, [int]$X, [int]$Y) {
    $halo = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(185, 248, 238, 196))
    $ring = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(235, 34, 53, 48), 4)
    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        $Graphics.FillEllipse($halo, $X - 7, $Y - 7, 78, 78)
        $Graphics.DrawEllipse($ring, $X - 7, $Y - 7, 78, 78)
        $Graphics.DrawImage($image, (New-Object System.Drawing.Rectangle($X, $Y, 64, 64)))
    }
    finally {
        $image.Dispose()
        $halo.Dispose()
        $ring.Dispose()
    }
}

function Draw-ReviewChrome([System.Drawing.Graphics]$Graphics, [string]$OptionLetter, [System.Drawing.Color]$Accent) {
    $hudBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(224, 13, 35, 46))
    $safeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(38, 248, 229, 164))
    $safePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 248, 229, 164), 3)
    $promptPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(225, 58, 207, 198), 3)
    $accentPen = New-Object System.Drawing.Pen($Accent, 5)
    $labelBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 251, 238, 194))
    $font = New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    try {
        $Graphics.FillRectangle($hudBrush, 16, 14, 1247, 119)
        $Graphics.FillRectangle($hudBrush, 16, 685, 1247, 101)
        $Graphics.FillRectangle($safeBrush, 230, 144, 819, 136)
        $Graphics.DrawRectangle($safePen, 230, 144, 819, 136)
        $Graphics.DrawRectangle($promptPen, 420, 292, 440, 44)
        $Graphics.DrawRectangle($accentPen, 3, 3, 1273, 793)
        $Graphics.DrawString("REVIEW ONLY  $OptionLetter", $font, $labelBrush, 36, 38)
    }
    finally {
        $hudBrush.Dispose()
        $safeBrush.Dispose()
        $safePen.Dispose()
        $promptPen.Dispose()
        $accentPen.Dispose()
        $labelBrush.Dispose()
        $font.Dispose()
    }
}

function New-OptionPreview([string]$Option, [string]$OutputPath) {
    $bitmap = New-OpaqueCanvas 1280 800 ([System.Drawing.Color]::FromArgb(255, 13, 35, 46))
    $graphics = New-Graphics $bitmap
    $coast = [System.Drawing.Image]::FromFile($coastPath)
    $barrier = [System.Drawing.Image]::FromFile($barrierPath)
    try {
        if ($Option -eq 'C') {
            $source = New-Object System.Drawing.Rectangle(0, 42, 1519, 855)
            $destination = New-Object System.Drawing.Rectangle(0, 40, 1280, 720)
            $graphics.DrawImage($coast, $destination, $source, [System.Drawing.GraphicsUnit]::Pixel)
        }
        else {
            $graphics.DrawImage($coast, (New-Object System.Drawing.Rectangle(0, 40, 1280, 720)))
        }

        if ($Option -eq 'B') {
            $upper = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(52, 17, 57, 64))
            $lower = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(78, 8, 40, 47))
            $lane = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(34, 255, 229, 153))
            try {
                $graphics.FillRectangle($upper, 0, 40, 1280, 390)
                $graphics.FillRectangle($lane, 160, 430, 820, 230)
                $graphics.FillRectangle($lower, 0, 665, 1280, 95)
            }
            finally {
                $upper.Dispose()
                $lower.Dispose()
                $lane.Dispose()
            }
        }

        $swimPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(245, 79, 220, 234), 6)
        $swimPen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
        try {
            $swimX = if ($Option -eq 'C') { 390 } else { 338 }
            $graphics.DrawLine($swimPen, $swimX, 444, $swimX, 652)
        }
        finally {
            $swimPen.Dispose()
        }

        $barrierX = if ($Option -eq 'A') { 955 } elseif ($Option -eq 'B') { 820 } else { 875 }
        $graphics.DrawImage($barrier, (New-Object System.Drawing.Rectangle($barrierX, 414, 205, 205)))

        Draw-Node $graphics (Join-Path $iconRoot 'scrap.png') 170 540
        Draw-Node $graphics (Join-Path $iconRoot 'stone.png') 285 520
        Draw-Node $graphics (Join-Path $iconRoot 'food.png') 478 526
        Draw-Node $graphics (Join-Path $iconRoot 'wood.png') 735 520
        Draw-KimEnvelope $graphics 616 625

        $accent = if ($Option -eq 'A') {
            [System.Drawing.Color]::FromArgb(235, 248, 190, 65)
        } elseif ($Option -eq 'B') {
            [System.Drawing.Color]::FromArgb(235, 72, 214, 185)
        } else {
            [System.Drawing.Color]::FromArgb(235, 76, 182, 240)
        }
        Draw-ReviewChrome $graphics $Option $accent
        $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $barrier.Dispose()
        $coast.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$optionAPath = Join-Path $OutputDirectory 'option-a-current-balanced-1280x800.png'
$optionBPath = Join-Path $OutputDirectory 'option-b-gameplay-band-contrast-1280x800.png'
$optionCPath = Join-Path $OutputDirectory 'option-c-shoreline-first-1280x800.png'
New-OptionPreview 'A' $optionAPath
New-OptionPreview 'B' $optionBPath
New-OptionPreview 'C' $optionCPath

$boardPath = Join-Path $OutputDirectory 'wave14-coast-forest-review-board.png'
$board = New-OpaqueCanvas 3840 2300 ([System.Drawing.Color]::FromArgb(255, 12, 31, 40))
$graphics = New-Graphics $board
$titleFont = New-Object System.Drawing.Font('Segoe UI', 38, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$headerFont = New-Object System.Drawing.Font('Segoe UI', 25, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$bodyFont = New-Object System.Drawing.Font('Segoe UI', 19, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 249, 233, 187))
$mutedBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 178, 205, 205))
$panelBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 24, 54, 63))
$tealPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 72, 214, 185), 4)
try {
    $graphics.DrawString('WAVE 14 - COAST / FOREST / SHALLOW-WATER REVIEW', $titleFont, $textBrush, 54, 28)
    $graphics.DrawString('Existing art + current runtime evidence + three unselected presentation treatments. Runtime allowlist: EMPTY.', $bodyFont, $mutedBrush, 58, 82)

    $graphics.FillRectangle($panelBrush, 40, 130, 1488, 880)
    $graphics.DrawString('FORGE SOURCE  1672x941', $headerFont, $textBrush, 62, 146)
    $coast = [System.Drawing.Image]::FromFile($coastPath)
    try { $graphics.DrawImage($coast, (New-Object System.Drawing.Rectangle(60, 194, 1448, 815))) } finally { $coast.Dispose() }

    $graphics.FillRectangle($panelBrush, 1550, 130, 2250, 880)
    $graphics.DrawString('CURRENT 1280x800 PROTOTYPE CAPTURES - ART NOT YET CONNECTED', $headerFont, $textBrush, 1572, 146)
    $exploration = [System.Drawing.Image]::FromFile($explorationPath)
    $swimming = [System.Drawing.Image]::FromFile($swimmingPath)
    try {
        $graphics.DrawImage($exploration, (New-Object System.Drawing.Rectangle(1570, 194, 1088, 680)))
        $graphics.DrawImage($swimming, (New-Object System.Drawing.Rectangle(2680, 194, 1088, 680)))
    }
    finally {
        $exploration.Dispose()
        $swimming.Dispose()
    }
    $graphics.DrawString('Finding: prototype geometry proves flow states, but cannot prove final background/object contrast.', $bodyFont, $mutedBrush, 1572, 902)

    $options = @(
        [pscustomobject]@{ Id='background.coast-forest.current-balanced'; Path=$optionAPath; X=55; Header='A  CURRENT BALANCED'; Note='Preserves the painting. Risk: right-side barrier can merge with foliage / inventory rail.' },
        [pscustomobject]@{ Id='background.coast-forest.gameplay-band-contrast'; Path=$optionBPath; X=1305; Header='B  GAMEPLAY-BAND CONTRAST'; Note='Clearest Kim, nodes and barrier. Tradeoff: slightly flatter depth; needs separated grading layers.' },
        [pscustomobject]@{ Id='background.coast-forest.shoreline-first'; Path=$optionCPath; X=2555; Header='C  SHORELINE-FIRST'; Note='Best swim entry and shallow-water share. Tradeoff: forest route has less horizontal breathing room.' }
    )
    foreach ($option in $options) {
        $graphics.FillRectangle($panelBrush, $option.X, 1050, 1225, 1120)
        $graphics.DrawString($option.Header, $headerFont, $textBrush, $option.X + 22, 1072)
        $graphics.DrawString($option.Id, $bodyFont, $mutedBrush, $option.X + 22, 1112)
        $preview = [System.Drawing.Image]::FromFile($option.Path)
        $previewRect = [System.Drawing.Rectangle]::new([int]($option.X + 28), 1160, 1169, 731)
        try { $graphics.DrawImage($preview, $previewRect) } finally { $preview.Dispose() }
        $graphics.DrawRectangle($tealPen, $option.X + 28, 1160, 1168, 730)
        $noteRect = New-Object System.Drawing.RectangleF([single]($option.X + 28), [single]1925, [single]1160, [single]130)
        $graphics.DrawString($option.Note, $bodyFont, $textBrush, $noteRect)
    }
    $graphics.DrawString('Mockups use a proportion/color proxy for Kim and adopted transparent resource/barrier art. Review labels and capture text are evidence only, never runtime sprites.', $bodyFont, $mutedBrush, 74, 2210)
    $graphics.DrawString('SELECTION GATE: choose one exact option ID later; until then all three remain review-only and no package/runtime connection is allowed.', $bodyFont, $textBrush, 74, 2244)
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
