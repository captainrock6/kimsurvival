param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'wave17-hazard-escape-ending-selection-board.png')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$hazardAtlasPath = Join-Path $repoRoot 'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-atlas.png'
$hazardReadabilityPath = Join-Path $repoRoot 'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-readability-32-64.png'
$escapePath = Join-Path $repoRoot 'Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-route-signature-a-1280x800.png'
$endingPath = Join-Path $repoRoot 'Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-triptych-a-1280x800.png'

$sourcePaths = @($hazardAtlasPath, $hazardReadabilityPath, $escapePath, $endingPath)
foreach ($sourcePath in $sourcePaths) {
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Missing review source: $sourcePath"
    }
}
$width = 3200
$height = 3000
$bitmap = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$bitmap.SetResolution(96, 96)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

$navy = [System.Drawing.ColorTranslator]::FromHtml('#102F38')
$deepNavy = [System.Drawing.ColorTranslator]::FromHtml('#09232B')
$cream = [System.Drawing.ColorTranslator]::FromHtml('#FFF4D7')
$paper = [System.Drawing.ColorTranslator]::FromHtml('#F7E8C2')
$teal = [System.Drawing.ColorTranslator]::FromHtml('#2C9A92')
$orange = [System.Drawing.ColorTranslator]::FromHtml('#F08A4B')
$gold = [System.Drawing.ColorTranslator]::FromHtml('#E8B85B')
$red = [System.Drawing.ColorTranslator]::FromHtml('#C9504E')
$muted = [System.Drawing.ColorTranslator]::FromHtml('#AEC3BD')
$white = [System.Drawing.Color]::White
$black = [System.Drawing.Color]::Black

$fontFamilyName = 'Malgun Gothic'
$fontTitle = New-Object System.Drawing.Font($fontFamilyName, 38, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fontSection = New-Object System.Drawing.Font($fontFamilyName, 26, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fontHeading = New-Object System.Drawing.Font($fontFamilyName, 20, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fontBody = New-Object System.Drawing.Font($fontFamilyName, 17, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$fontSmall = New-Object System.Drawing.Font($fontFamilyName, 14, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$fontMono = New-Object System.Drawing.Font('Consolas', 15, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)

$brushNavy = New-Object System.Drawing.SolidBrush($navy)
$brushDeep = New-Object System.Drawing.SolidBrush($deepNavy)
$brushCream = New-Object System.Drawing.SolidBrush($cream)
$brushPaper = New-Object System.Drawing.SolidBrush($paper)
$brushTeal = New-Object System.Drawing.SolidBrush($teal)
$brushOrange = New-Object System.Drawing.SolidBrush($orange)
$brushGold = New-Object System.Drawing.SolidBrush($gold)
$brushRed = New-Object System.Drawing.SolidBrush($red)
$brushMuted = New-Object System.Drawing.SolidBrush($muted)
$brushWhite = New-Object System.Drawing.SolidBrush($white)
$penCream = New-Object System.Drawing.Pen($cream, 3)
$penTeal = New-Object System.Drawing.Pen($teal, 4)
$penOrange = New-Object System.Drawing.Pen($orange, 4)
$penMuted = New-Object System.Drawing.Pen($muted, 2)
$penDash = New-Object System.Drawing.Pen($gold, 3)
$penDash.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash

function Draw-TextBlock {
    param(
        [string]$Text,
        [System.Drawing.Font]$Font,
        [System.Drawing.Brush]$Brush,
        [int]$X,
        [int]$Y,
        [int]$W,
        [int]$H
    )
    $format = New-Object System.Drawing.StringFormat
    $format.Trimming = [System.Drawing.StringTrimming]::EllipsisWord
    $format.FormatFlags = [System.Drawing.StringFormatFlags]::LineLimit
    $graphics.DrawString($Text, $Font, $Brush, (New-Object System.Drawing.RectangleF($X, $Y, $W, $H)), $format)
    $format.Dispose()
}

function Draw-Checker {
    param([int]$X, [int]$Y, [int]$W, [int]$H, [int]$Cell = 24)
    $a = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#D5DED8'))
    $b = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#B9C9C2'))
    for ($yy = 0; $yy -lt $H; $yy += $Cell) {
        for ($xx = 0; $xx -lt $W; $xx += $Cell) {
            $brush = if ((([int]($xx / $Cell) + [int]($yy / $Cell)) % 2) -eq 0) { $a } else { $b }
            $graphics.FillRectangle($brush, $X + $xx, $Y + $yy, [Math]::Min($Cell, $W - $xx), [Math]::Min($Cell, $H - $yy))
        }
    }
    $a.Dispose()
    $b.Dispose()
}

function Draw-Badge {
    param([string]$Text, [int]$X, [int]$Y, [int]$W, [System.Drawing.Brush]$Fill)
    $graphics.FillRectangle($Fill, $X, $Y, $W, 30)
    Draw-TextBlock $Text $fontSmall $brushDeep ($X + 8) ($Y + 5) ($W - 16) 22
}

function Draw-ScreenDiagram {
    param(
        [int]$X,
        [int]$Y,
        [int]$W,
        [int]$H,
        [string]$Mode,
        [string]$Caption
    )
    $graphics.FillRectangle($brushDeep, $X, $Y, $W, $H)
    $graphics.DrawRectangle($penMuted, $X, $Y, $W, $H)
    $hudH = [int]($H * 0.10)
    $graphics.FillRectangle($brushTeal, $X + 12, $Y + 12, $W - 24, $hudH)
    if ($Mode -eq 'hazard') {
        $bandY = $Y + [int]($H * 0.57)
        $graphics.DrawLine($penDash, $X + 20, $bandY, $X + $W - 20, $bandY)
        $graphics.FillEllipse($brushOrange, $X + [int]($W * 0.47), $bandY - 28, 56, 56)
        $graphics.DrawEllipse($penCream, $X + [int]($W * 0.47) - 10, $bandY - 38, 76, 76)
        Draw-TextBlock 'FX/state marker' $fontSmall $brushCream ($X + 20) ($bandY + 48) ($W - 40) 24
    }
    elseif ($Mode -eq 'popup') {
        $popupX = $X + [int]($W * 0.14)
        $popupY = $Y + [int]($H * 0.16)
        $popupW = [int]($W * 0.72)
        $popupH = [int]($H * 0.70)
        $graphics.FillRectangle($brushPaper, $popupX, $popupY, $popupW, $popupH)
        $graphics.DrawRectangle($penOrange, $popupX, $popupY, $popupW, $popupH)
        Draw-TextBlock 'situational facility popup' $fontSmall $brushDeep ($popupX + 14) ($popupY + 12) ($popupW - 28) 26
    }
    elseif ($Mode -eq 'fullscreen') {
        $frameX = $X + 12
        $frameY = $Y + 12
        $frameW = $W - 24
        $frameH = $H - 24
        $graphics.DrawRectangle($penOrange, $frameX, $frameY, $frameW, $frameH)
        $third = [int](($frameW - 40) / 3)
        for ($i = 0; $i -lt 3; $i++) {
            $px = $frameX + 10 + ($i * ($third + 10))
            $graphics.DrawRectangle($penCream, $px, $frameY + 54, $third, [int]($frameH * 0.55))
        }
        Draw-TextBlock 'terminal / results overlay' $fontSmall $brushCream ($frameX + 12) ($frameY + 12) ($frameW - 24) 28
    }
    Draw-TextBlock $Caption $fontSmall $brushMuted ($X + 10) ($Y + $H + 5) $W 46
}

try {
    $graphics.Clear($navy)
    $graphics.FillRectangle($brushDeep, 0, 0, $width, 120)
    Draw-TextBlock 'WAVE 17  |  HAZARD · ESCAPE · ENDING  SELECTION PACKET' $fontTitle $brushCream 48 22 2500 52
    Draw-Badge 'REVIEW ONLY' 2710 24 210 $brushGold
    Draw-Badge 'NO PIXEL EDIT' 2930 24 220 $brushTeal
    Draw-TextBlock 'Three independent assets. The sections below are NOT one combined game screen.' $fontBody $brushMuted 52 77 2400 32

    # Hazard: exact 1:1 source atlas and readability sheet.
    $hazardY = 142
    Draw-TextBlock '01  effect.survival-hazards.phase-silhouette-a' $fontSection $brushCream 48 $hazardY 1500 38
    Draw-TextBlock 'job_20260823160305_ef04b0f3  ·  decision=review  ·  selectedCandidate=null' $fontMono $brushMuted 1580 ($hazardY + 4) 1550 30
    $atlasX = 48
    $atlasY = $hazardY + 46
    Draw-Checker $atlasX $atlasY 1024 768 24
    $hazardAtlas = [System.Drawing.Image]::FromFile($hazardAtlasPath)
    $graphics.DrawImageUnscaled($hazardAtlas, $atlasX, $atlasY)
    $graphics.DrawRectangle($penCream, $atlasX, $atlasY, 1024, 768)
    Draw-Badge '1:1 SOURCE · 1024×768 · TRUE ALPHA' ($atlasX + 12) ($atlasY + 12) 340 $brushGold

    $readX = 1100
    $readY = $atlasY
    $hazardReadability = [System.Drawing.Image]::FromFile($hazardReadabilityPath)
    $graphics.DrawImageUnscaled($hazardReadability, $readX, $readY)
    $graphics.DrawRectangle($penCream, $readX, $readY, 1024, 512)
    Draw-Badge '1:1 READABILITY · 32 / 64 PX' ($readX + 12) ($readY + 12) 300 $brushTeal
    Draw-TextBlock 'COLOR-INDEPENDENT PHASE GRAMMAR' $fontHeading $brushGold $readX ($readY + 532) 720 30
    Draw-TextBlock "telegraph  dashed triangle + pulse arcs`noccurrence  jagged burst`nmitigation  shield + chevron/check`nrecovery  round field + rising leaf/sun" $fontBody $brushCream $readX ($readY + 568) 760 132
    Draw-TextBlock 'Glyph/focus slot: 44×44 minimum · raster body text: none · locales: KO / EN / qps-long' $fontBody $brushMuted $readX ($readY + 706) 980 56

    $detailX = 2160
    Draw-TextBlock 'WHAT TO COMPARE' $fontHeading $brushOrange $detailX $readY 940 32
    Draw-TextBlock "• Does every phase read without relying on color?`n• Are injury / storm / food theft distinct at 32 and 64 px?`n• Does the comic impact remain non-gory and brief?" $fontBody $brushCream $detailX ($readY + 42) 930 118
    Draw-TextBlock 'EXPECTED RUNTIME PLACEMENT' $fontHeading $brushOrange $detailX ($readY + 176) 940 32
    Draw-ScreenDiagram $detailX ($readY + 214) 920 420 'hazard' 'Attach near the affected world action or HUD event. Never use as a persistent full-screen panel.'
    Draw-Badge 'allowlist=[]  ·  package=false  ·  runtime=false' $detailX ($readY + 696) 590 $brushRed

    # Escape: exact 1280x800 candidate, isolated from other assets.
    $escapeY = 982
    $graphics.DrawLine($penMuted, 48, $escapeY - 18, 3152, $escapeY - 18)
    Draw-TextBlock '02  ui.escape-project-progress.route-signature-a' $fontSection $brushCream 48 $escapeY 1550 38
    Draw-TextBlock 'job_20260823160324_1de3b748  ·  decision=review  ·  selectedCandidate=null' $fontMono $brushMuted 1580 ($escapeY + 4) 1550 30
    $escapeX = 48
    $escapeImageY = $escapeY + 46
    $escapeImage = [System.Drawing.Image]::FromFile($escapePath)
    $graphics.DrawImageUnscaled($escapeImage, $escapeX, $escapeImageY)
    $graphics.DrawRectangle($penCream, $escapeX, $escapeImageY, 1280, 800)
    Draw-Badge '1:1 SOURCE · 1280×800' ($escapeX + 12) ($escapeImageY + 12) 250 $brushGold

    $escapeInfoX = 1370
    Draw-TextBlock 'WHAT TO COMPARE' $fontHeading $brushOrange $escapeInfoX $escapeImageY 820 32
    Draw-TextBlock "• Smoke and radio must feel mechanically different.`n• Raft / flare / beacon remain data routes, not cloned buttons.`n• Conditions, weather window and risk should scan before confirmation." $fontBody $brushCream $escapeInfoX ($escapeImageY + 42) 820 118
    Draw-TextBlock 'SAFE RECTS / INPUT' $fontHeading $brushOrange $escapeInfoX ($escapeImageY + 176) 820 32
    Draw-TextBlock "outer L36 R36 T38 B38`nroute rail  x88 y108 w250 h584`ndetail      x360 y108 w830 h584`nTMP title   x390 y132 w500 h54`nfocus/glyph 44×44 min`nqps-long    150% · wrap then vertical reflow" $fontMono $brushCream $escapeInfoX ($escapeImageY + 214) 790 154
    Draw-TextBlock 'EXPECTED RUNTIME PLACEMENT' $fontHeading $brushOrange 2220 $escapeImageY 900 32
    Draw-ScreenDiagram 2220 ($escapeImageY + 42) 900 500 'popup' 'Centered only during direct escape-facility interaction. Not a persistent camp dashboard.'
    Draw-Badge 'allowlist=[]  ·  package=false  ·  runtime=false' 2220 ($escapeImageY + 610) 590 $brushRed
    Draw-TextBlock 'Glyph slots are engine-owned. KO/EN/qps-long text remains TMP; no translated body copy is baked into the source.' $fontBody $brushMuted 1370 ($escapeImageY + 696) 1700 62

    # Ending: exact 1280x800 candidate, isolated from other assets.
    $endingY = 1848
    $graphics.DrawLine($penMuted, 48, $endingY - 18, 3152, $endingY - 18)
    Draw-TextBlock '03  ui.ending-comic.triptych-a' $fontSection $brushCream 48 $endingY 1450 38
    Draw-TextBlock 'job_20260823160342_eceb3933  ·  decision=review  ·  selectedCandidate=null' $fontMono $brushMuted 1580 ($endingY + 4) 1550 30
    $endingX = 48
    $endingImageY = $endingY + 46
    $endingImage = [System.Drawing.Image]::FromFile($endingPath)
    $graphics.DrawImageUnscaled($endingImage, $endingX, $endingImageY)
    $graphics.DrawRectangle($penCream, $endingX, $endingImageY, 1280, 800)
    Draw-Badge '1:1 SOURCE · 1280×800' ($endingX + 12) ($endingImageY + 12) 250 $brushGold

    $endingInfoX = 1370
    Draw-TextBlock 'WHAT TO COMPARE' $fontHeading $brushOrange $endingInfoX $endingImageY 820 32
    Draw-TextBlock "• Is the setup → close-up → result rhythm immediately clear?`n• Do normal / comic / rare / Day50 outcomes differ by frame shape and pattern?`n• Is the verdict evidence band readable without crowding the comic?" $fontBody $brushCream $endingInfoX ($endingImageY + 42) 820 122
    Draw-TextBlock 'SAFE RECTS / CONTENT RULES' $fontHeading $brushOrange $endingInfoX ($endingImageY + 180) 820 32
    Draw-TextBlock "setup   x64  y154 w500 h344`ncloseup polygon bounds x592 y144 → x866 y506`nresult  x888 y154 w328 h344`nverdict x64  y572 w1152 h142`nfocus/glyph 44×44 min`nqps-long 150% · text is TMP only" $fontMono $brushCream $endingInfoX ($endingImageY + 218) 800 154
    Draw-TextBlock 'EXPECTED RUNTIME PLACEMENT' $fontHeading $brushOrange 2220 $endingImageY 900 32
    Draw-ScreenDiagram 2220 ($endingImageY + 42) 900 500 'fullscreen' 'Full-screen terminal/results overlay. Gallery replay may reuse the frame; never place in the camp HUD.'
    Draw-Badge 'allowlist=[]  ·  package=false  ·  runtime=false' 2220 ($endingImageY + 610) 590 $brushRed
    Draw-TextBlock 'Optional inserts: at most one dominant behavior + one event scar. Modifiers do not change endingId.' $fontBody $brushMuted 1370 ($endingImageY + 696) 1700 62

    # Explicit response contract.
    $footerY = 2730
    $graphics.FillRectangle($brushDeep, 0, $footerY, $width, $height - $footerY)
    Draw-TextBlock 'SELECTION RESPONSE — decide each stable ID independently' $fontSection $brushCream 48 ($footerY + 24) 1500 38
    Draw-TextBlock '채택 / 수정 — 변경점 / 보류 / 거절 — 이유' $fontHeading $brushGold 2220 ($footerY + 28) 900 34
    Draw-TextBlock "effect.survival-hazards.phase-silhouette-a: [결정]`nui.escape-project-progress.route-signature-a: [결정]`nui.ending-comic.triptych-a: [결정]" $fontMono $brushCream 48 ($footerY + 76) 1760 104
    Draw-TextBlock 'Until a new explicit user decision: decision=review · selectedCandidate=null · runtimeAllowlist=[] · packageAllowed=false · runtimeConnectAllowed=false' $fontSmall $brushMuted 48 ($footerY + 204) 3060 32

    $outputDirectory = Split-Path -Parent $OutputPath
    if (-not (Test-Path -LiteralPath $outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory | Out-Null
    }
    $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    if ($hazardAtlas) { $hazardAtlas.Dispose() }
    if ($hazardReadability) { $hazardReadability.Dispose() }
    if ($escapeImage) { $escapeImage.Dispose() }
    if ($endingImage) { $endingImage.Dispose() }
    $graphics.Dispose()
    $bitmap.Dispose()
    @($fontTitle, $fontSection, $fontHeading, $fontBody, $fontSmall, $fontMono,
      $brushNavy, $brushDeep, $brushCream, $brushPaper, $brushTeal, $brushOrange,
      $brushGold, $brushRed, $brushMuted, $brushWhite,
      $penCream, $penTeal, $penOrange, $penMuted, $penDash) | ForEach-Object {
        if ($_ -and ($_ -is [System.IDisposable])) { $_.Dispose() }
    }
}
