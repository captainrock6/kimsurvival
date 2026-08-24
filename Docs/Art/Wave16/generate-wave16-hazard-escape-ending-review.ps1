param(
    [Parameter(Mandatory = $true)]
    [string]$OutputRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$hazardRoot = Join-Path $OutputRoot 'hazards'
$escapeRoot = Join-Path $OutputRoot 'escape'
$endingRoot = Join-Path $OutputRoot 'ending'
New-Item -ItemType Directory -Force -Path $hazardRoot, $escapeRoot, $endingRoot | Out-Null

function Color([string]$hex, [int]$alpha = 255) {
    $h = $hex.TrimStart('#')
    return [System.Drawing.Color]::FromArgb(
        $alpha,
        [Convert]::ToInt32($h.Substring(0, 2), 16),
        [Convert]::ToInt32($h.Substring(2, 2), 16),
        [Convert]::ToInt32($h.Substring(4, 2), 16)
    )
}

function Brush([string]$hex, [int]$alpha = 255) {
    return [System.Drawing.SolidBrush]::new((Color $hex $alpha))
}

function Pen([string]$hex, [float]$width, [int]$alpha = 255) {
    $p = [System.Drawing.Pen]::new((Color $hex $alpha), $width)
    $p.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $p.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $p.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    return $p
}

function New-Canvas([int]$width, [int]$height, [bool]$transparent = $false) {
    $bmp = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $bmp.SetResolution(96, 96)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    if ($transparent) { $g.Clear([System.Drawing.Color]::Transparent) } else { $g.Clear((Color '#102F38')) }
    return @{ Bitmap = $bmp; Graphics = $g }
}

function Save-Png($canvas, [string]$path) {
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Bitmap.Dispose()
}

function RoundedRect([System.Drawing.Graphics]$g, [System.Drawing.RectangleF]$r, [float]$radius, [string]$fill, [string]$stroke, [float]$strokeWidth, [int]$alpha = 255) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $d = [Math]::Min($radius * 2, [Math]::Min($r.Width, $r.Height))
    $path.AddArc($r.X, $r.Y, $d, $d, 180, 90)
    $path.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
    $path.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    if ($fill) { $b = Brush $fill $alpha; $g.FillPath($b, $path); $b.Dispose() }
    if ($stroke -and $strokeWidth -gt 0) { $p = Pen $stroke $strokeWidth; $g.DrawPath($p, $path); $p.Dispose() }
    $path.Dispose()
}

function Draw-Text([System.Drawing.Graphics]$g, [string]$text, [float]$x, [float]$y, [float]$size, [string]$hex = '#F4E0AE', [bool]$bold = $false) {
    $style = if ($bold) { [System.Drawing.FontStyle]::Bold } else { [System.Drawing.FontStyle]::Regular }
    $font = [System.Drawing.Font]::new('Arial', $size, $style, [System.Drawing.GraphicsUnit]::Pixel)
    $b = Brush $hex
    $g.DrawString($text, $font, $b, $x, $y)
    $b.Dispose(); $font.Dispose()
}

function Draw-Hatch([System.Drawing.Graphics]$g, [System.Drawing.RectangleF]$r, [string]$hex, [float]$step = 12, [int]$alpha = 120) {
    $state = $g.Save()
    $g.SetClip($r)
    $p = Pen $hex 3 $alpha
    for ($x = $r.X - $r.Height; $x -lt $r.Right; $x += $step) { $g.DrawLine($p, $x, $r.Bottom, $x + $r.Height, $r.Y) }
    $p.Dispose(); $g.Restore($state)
}

function Draw-SafeRect([System.Drawing.Graphics]$g, [System.Drawing.RectangleF]$r, [string]$stroke = '#4DB7BF', [int]$alpha = 220) {
    $p = Pen $stroke 2 $alpha
    $p.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
    $g.DrawRectangle($p, $r.X, $r.Y, $r.Width, $r.Height)
    $p.Dispose()
}

function Draw-Check([System.Drawing.Graphics]$g, [float]$x, [float]$y, [float]$s, [string]$hex = '#F4E0AE') {
    $p = Pen $hex ([Math]::Max(3, $s * 0.12))
    $g.DrawLines($p, [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($x, $y + $s * 0.52),
        [System.Drawing.PointF]::new($x + $s * 0.34, $y + $s * 0.82),
        [System.Drawing.PointF]::new($x + $s, $y)
    ))
    $p.Dispose()
}

function Draw-GlyphSlot([System.Drawing.Graphics]$g, [float]$x, [float]$y, [float]$s = 44) {
    RoundedRect $g ([System.Drawing.RectangleF]::new($x, $y, $s, $s)) 10 '#F3BE4D' '#17353B' 4
    $p = Pen '#17353B' 4
    $g.DrawEllipse($p, $x + $s * 0.31, $y + $s * 0.31, $s * 0.38, $s * 0.38)
    $p.Dispose()
}

function Draw-Burst([System.Drawing.Graphics]$g, [float]$cx, [float]$cy, [float]$outer, [float]$inner, [string]$fill, [string]$stroke) {
    $pts = [System.Drawing.PointF[]]::new(20)
    for ($i = 0; $i -lt 20; $i++) {
        $a = (-90 + $i * 18) * [Math]::PI / 180
        $r = if (($i % 2) -eq 0) { $outer } else { $inner }
        $pts[$i] = [System.Drawing.PointF]::new($cx + [Math]::Cos($a) * $r, $cy + [Math]::Sin($a) * $r)
    }
    $b = Brush $fill 230; $g.FillPolygon($b, $pts); $b.Dispose()
    $p = Pen $stroke 6; $g.DrawPolygon($p, $pts); $p.Dispose()
}

function Draw-PhaseFrame([System.Drawing.Graphics]$g, [float]$cx, [float]$cy, [float]$s, [string]$phase) {
    switch ($phase) {
        'telegraph' {
            $p = Pen '#F3BE4D' ([Math]::Max(4, $s * 0.04))
            $p.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
            $pts = [System.Drawing.PointF[]]@(
                [System.Drawing.PointF]::new($cx, $cy - $s * 0.44),
                [System.Drawing.PointF]::new($cx + $s * 0.42, $cy + $s * 0.34),
                [System.Drawing.PointF]::new($cx - $s * 0.42, $cy + $s * 0.34)
            )
            $g.DrawPolygon($p, $pts); $p.Dispose()
            $arc = Pen '#F3BE4D' ([Math]::Max(3, $s * 0.03)) 190
            $g.DrawArc($arc, $cx - $s * 0.50, $cy - $s * 0.50, $s, $s, 206, 42)
            $g.DrawArc($arc, $cx - $s * 0.42, $cy - $s * 0.42, $s * 0.84, $s * 0.84, 292, 42)
            $arc.Dispose()
        }
        'occurrence' { Draw-Burst $g $cx $cy ($s * 0.48) ($s * 0.36) '#E8682A' '#17353B' }
        'mitigation' {
            $pts = [System.Drawing.PointF[]]@(
                [System.Drawing.PointF]::new($cx, $cy - $s * 0.47),
                [System.Drawing.PointF]::new($cx + $s * 0.39, $cy - $s * 0.30),
                [System.Drawing.PointF]::new($cx + $s * 0.31, $cy + $s * 0.25),
                [System.Drawing.PointF]::new($cx, $cy + $s * 0.47),
                [System.Drawing.PointF]::new($cx - $s * 0.31, $cy + $s * 0.25),
                [System.Drawing.PointF]::new($cx - $s * 0.39, $cy - $s * 0.30)
            )
            $b = Brush '#168C84' 220; $g.FillPolygon($b, $pts); $b.Dispose()
            $p = Pen '#17353B' ([Math]::Max(4, $s * 0.045)); $g.DrawPolygon($p, $pts); $p.Dispose()
        }
        'recovery' {
            $b = Brush '#6D9A56' 220; $g.FillEllipse($b, $cx - $s * 0.44, $cy - $s * 0.44, $s * 0.88, $s * 0.88); $b.Dispose()
            $p = Pen '#17353B' ([Math]::Max(4, $s * 0.045)); $g.DrawEllipse($p, $cx - $s * 0.44, $cy - $s * 0.44, $s * 0.88, $s * 0.88); $p.Dispose()
            $leaf = Brush '#F3BE4D'
            $g.FillEllipse($leaf, $cx + $s * 0.18, $cy - $s * 0.42, $s * 0.20, $s * 0.32)
            $leaf.Dispose()
        }
    }
}

function Draw-Hazard([System.Drawing.Graphics]$g, [string]$hazard, [string]$phase, [System.Drawing.RectangleF]$r) {
    $s = [Math]::Min($r.Width, $r.Height)
    $cx = $r.X + $r.Width / 2; $cy = $r.Y + $r.Height / 2
    Draw-PhaseFrame $g $cx $cy ($s * 0.90) $phase
    $outline = Pen '#17353B' ([Math]::Max(4, $s * 0.035))
    switch ($hazard) {
        'injury' {
            $state = $g.Save(); $g.TranslateTransform($cx, $cy); $g.RotateTransform(-38)
            RoundedRect $g ([System.Drawing.RectangleF]::new(-$s * 0.29, -$s * 0.12, $s * 0.58, $s * 0.24)) ($s * 0.07) '#F4E0AE' '#17353B' ([Math]::Max(4, $s * 0.035))
            $pad = Brush '#E8682A'; $g.FillRectangle($pad, -$s * 0.10, -$s * 0.13, $s * 0.20, $s * 0.26); $pad.Dispose()
            $holes = Brush '#17353B'
            foreach ($hx in @(-0.21, 0.19)) { foreach ($hy in @(-0.045, 0.045)) { $g.FillEllipse($holes, $s * $hx, $s * $hy, $s * 0.035, $s * 0.035) } }
            $holes.Dispose(); $g.Restore($state)
            if ($phase -eq 'mitigation') { Draw-Check $g ($cx - $s * 0.18) ($cy - $s * 0.06) ($s * 0.34) }
            if ($phase -eq 'recovery') { $p = Pen '#F4E0AE' ($s * 0.035); $g.DrawArc($p, $cx - $s * 0.22, $cy + $s * 0.10, $s * 0.44, $s * 0.20, 10, 160); $p.Dispose() }
        }
        'storm' {
            $cloud = Brush '#E7D39F'; $blue = Brush '#4A8FB8'
            $g.FillEllipse($cloud, $cx - $s * 0.25, $cy - $s * 0.19, $s * 0.30, $s * 0.26)
            $g.FillEllipse($cloud, $cx - $s * 0.04, $cy - $s * 0.28, $s * 0.36, $s * 0.34)
            $g.FillEllipse($cloud, $cx + $s * 0.18, $cy - $s * 0.16, $s * 0.25, $s * 0.23)
            $g.FillRectangle($cloud, $cx - $s * 0.24, $cy - $s * 0.08, $s * 0.62, $s * 0.18)
            $g.DrawArc($outline, $cx - $s * 0.25, $cy - $s * 0.19, $s * 0.30, $s * 0.26, 180, 210)
            $g.DrawArc($outline, $cx - $s * 0.04, $cy - $s * 0.28, $s * 0.36, $s * 0.34, 190, 210)
            $g.DrawLine($outline, $cx - $s * 0.22, $cy + $s * 0.10, $cx + $s * 0.36, $cy + $s * 0.10)
            $dropCount = if ($phase -eq 'occurrence') { 5 } else { 3 }
            for ($i = 0; $i -lt $dropCount; $i++) {
                $dx = $cx - $s * 0.24 + $i * $s * 0.12
                $g.DrawLine($outline, $dx, $cy + $s * 0.17, $dx - $s * 0.07, $cy + $s * 0.34)
                $g.FillEllipse($blue, $dx - $s * 0.10, $cy + $s * 0.28, $s * 0.09, $s * 0.13)
            }
            if ($phase -eq 'mitigation') { $roof = Pen '#F4E0AE' ($s * 0.05); $g.DrawLines($roof, [System.Drawing.PointF[]]@([System.Drawing.PointF]::new($cx-$s*0.32,$cy+$s*0.29),[System.Drawing.PointF]::new($cx,$cy+$s*0.10),[System.Drawing.PointF]::new($cx+$s*0.32,$cy+$s*0.29))); $roof.Dispose() }
            if ($phase -eq 'recovery') { $sun = Brush '#F3BE4D'; $g.FillEllipse($sun, $cx + $s * 0.16, $cy - $s * 0.30, $s * 0.22, $s * 0.22); $sun.Dispose() }
            $cloud.Dispose(); $blue.Dispose()
        }
        'theft' {
            $crate = Brush '#D79548'; $dark = Brush '#17353B'; $cream = Brush '#F4E0AE'
            RoundedRect $g ([System.Drawing.RectangleF]::new($cx - $s * 0.29, $cy - $s * 0.03, $s * 0.58, $s * 0.35)) ($s * 0.035) '#D79548' '#17353B' ([Math]::Max(4, $s * 0.035))
            $g.DrawLine($outline, $cx - $s * 0.18, $cy - $s * 0.01, $cx + $s * 0.17, $cy + $s * 0.29)
            $g.DrawLine($outline, $cx + $s * 0.18, $cy - $s * 0.01, $cx - $s * 0.17, $cy + $s * 0.29)
            if ($phase -eq 'occurrence') { $g.DrawLine($outline, $cx - $s * 0.30, $cy - $s * 0.04, $cx + $s * 0.20, $cy - $s * 0.25); $g.FillEllipse($cream, $cx+$s*0.21,$cy-$s*0.22,$s*0.10,$s*0.08) }
            else { $g.DrawLine($outline, $cx - $s * 0.30, $cy - $s * 0.05, $cx + $s * 0.30, $cy - $s * 0.05) }
            for ($i = 0; $i -lt 3; $i++) {
                $px = $cx - $s * 0.31 + $i * $s * 0.17; $py = $cy - $s * 0.28 - ($i % 2) * $s * 0.06
                $g.FillEllipse($dark, $px, $py, $s * 0.075, $s * 0.065)
                $g.FillEllipse($dark, $px + $s * 0.02, $py - $s * 0.045, $s * 0.025, $s * 0.025)
            }
            if ($phase -eq 'mitigation') { RoundedRect $g ([System.Drawing.RectangleF]::new($cx-$s*0.10,$cy-$s*0.02,$s*0.20,$s*0.22)) ($s*0.03) '#F3BE4D' '#17353B' ($s*0.035); $g.DrawArc($outline,$cx-$s*0.07,$cy-$s*0.13,$s*0.14,$s*0.16,180,180) }
            if ($phase -eq 'recovery') { Draw-Check $g ($cx - $s * 0.12) ($cy + $s * 0.02) ($s * 0.25) }
            $crate.Dispose(); $dark.Dispose(); $cream.Dispose()
        }
    }
    $outline.Dispose()
}

function Draw-RouteGlyph([System.Drawing.Graphics]$g, [string]$route, [System.Drawing.RectangleF]$r, [string]$stroke = '#17353B') {
    $s = [Math]::Min($r.Width, $r.Height); $cx = $r.X + $r.Width / 2; $cy = $r.Y + $r.Height / 2
    $p = Pen $stroke ([Math]::Max(3, $s * 0.055)); $thin = Pen $stroke ([Math]::Max(2, $s * 0.035))
    switch ($route) {
        'smoke' {
            $g.DrawLine($p, $cx, $cy+$s*0.34, $cx, $cy-$s*0.04)
            $g.DrawEllipse($p,$cx-$s*0.15,$cy-$s*0.12,$s*0.30,$s*0.20)
            $g.DrawEllipse($p,$cx-$s*0.06,$cy-$s*0.30,$s*0.25,$s*0.23)
            $g.DrawArc($thin,$cx-$s*0.38,$cy-$s*0.37,$s*0.50,$s*0.24,190,140)
            for($i=0;$i -lt 3;$i++){$g.DrawEllipse($thin,$cx-$s*0.22+$i*$s*0.15,$cy+$s*0.16,$s*0.10,$s*0.10)}
        }
        'radio' {
            RoundedRect $g ([System.Drawing.RectangleF]::new($cx-$s*0.31,$cy-$s*0.20,$s*0.62,$s*0.44)) ($s*0.04) '#F4E0AE' $stroke ([Math]::Max(3,$s*0.05))
            $g.DrawLine($p,$cx+$s*0.18,$cy-$s*0.20,$cx+$s*0.31,$cy-$s*0.40)
            $g.DrawLines($thin,[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($cx-$s*0.23,$cy),[System.Drawing.PointF]::new($cx-$s*0.14,$cy-$s*0.08),[System.Drawing.PointF]::new($cx-$s*0.04,$cy+$s*0.08),[System.Drawing.PointF]::new($cx+$s*0.06,$cy-$s*0.06),[System.Drawing.PointF]::new($cx+$s*0.14,$cy)))
            $g.DrawEllipse($p,$cx+$s*0.12,$cy+$s*0.05,$s*0.10,$s*0.10)
        }
        'raft' {
            $hull=[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($cx-$s*0.36,$cy+$s*0.12),[System.Drawing.PointF]::new($cx+$s*0.36,$cy+$s*0.12),[System.Drawing.PointF]::new($cx+$s*0.24,$cy+$s*0.28),[System.Drawing.PointF]::new($cx-$s*0.26,$cy+$s*0.28));$g.DrawPolygon($p,$hull)
            $g.DrawLine($p,$cx,$cy+$s*0.10,$cx,$cy-$s*0.34)
            $sail=[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($cx+$s*0.02,$cy-$s*0.30),[System.Drawing.PointF]::new($cx+$s*0.02,$cy+$s*0.02),[System.Drawing.PointF]::new($cx+$s*0.27,$cy));$g.DrawPolygon($thin,$sail)
            $g.DrawArc($thin,$cx-$s*0.37,$cy+$s*0.24,$s*0.74,$s*0.20,190,160)
        }
        'flare' {
            $g.DrawLine($p,$cx-$s*0.08,$cy+$s*0.34,$cx+$s*0.10,$cy-$s*0.15)
            $g.DrawLine($p,$cx+$s*0.02,$cy+$s*0.38,$cx+$s*0.20,$cy-$s*0.11)
            Draw-Burst $g ($cx+$s*0.20) ($cy-$s*0.25) ($s*0.22) ($s*0.10) '#F3BE4D' $stroke
            $g.DrawArc($thin,$cx-$s*0.39,$cy-$s*0.39,$s*0.78,$s*0.78,215,82)
        }
        'beacon' {
            $g.DrawLine($p,$cx,$cy-$s*0.38,$cx-$s*0.25,$cy+$s*0.34)
            $g.DrawLine($p,$cx,$cy-$s*0.38,$cx+$s*0.25,$cy+$s*0.34)
            $g.DrawLine($thin,$cx-$s*0.17,$cy+$s*0.10,$cx+$s*0.17,$cy+$s*0.10)
            $g.DrawLine($thin,$cx-$s*0.11,$cy-$s*0.09,$cx+$s*0.11,$cy-$s*0.09)
            $g.DrawEllipse($p,$cx-$s*0.09,$cy-$s*0.46,$s*0.18,$s*0.18)
            $g.DrawArc($thin,$cx-$s*0.35,$cy-$s*0.55,$s*0.70,$s*0.40,200,140)
        }
    }
    $p.Dispose(); $thin.Dispose()
}

function Draw-CharacterPlaceholder([System.Drawing.Graphics]$g, [float]$x, [float]$y, [float]$scale, [string]$pose = 'stand') {
    $outline = Pen '#17353B' (5*$scale); $skin=Brush '#E8A06B'; $shirt=Brush '#E8682A'; $shorts=Brush '#526F45'; $hair=Brush '#1D2426'
    $g.FillEllipse($skin,$x+36*$scale,$y,$scale*46,$scale*52);$g.DrawEllipse($outline,$x+36*$scale,$y,$scale*46,$scale*52)
    $g.FillEllipse($hair,$x+32*$scale,$y-5*$scale,$scale*54,$scale*25)
    RoundedRect $g ([System.Drawing.RectangleF]::new($x+24*$scale,$y+46*$scale,70*$scale,88*$scale)) (12*$scale) '#E8682A' '#17353B' (5*$scale)
    $g.FillRectangle($shorts,$x+28*$scale,$y+123*$scale,62*$scale,40*$scale);$g.DrawRectangle($outline,$x+28*$scale,$y+123*$scale,62*$scale,40*$scale)
    if($pose -eq 'radio'){$g.DrawLine($outline,$x+83*$scale,$y+70*$scale,$x+122*$scale,$y+50*$scale);$g.DrawEllipse($outline,$x+118*$scale,$y+40*$scale,20*$scale,28*$scale)}
    else{$g.DrawLine($outline,$x+31*$scale,$y+76*$scale,$x+6*$scale,$y+122*$scale);$g.DrawLine($outline,$x+86*$scale,$y+76*$scale,$x+111*$scale,$y+122*$scale)}
    $g.DrawLine($outline,$x+42*$scale,$y+160*$scale,$x+32*$scale,$y+214*$scale);$g.DrawLine($outline,$x+76*$scale,$y+160*$scale,$x+88*$scale,$y+214*$scale)
    $outline.Dispose();$skin.Dispose();$shirt.Dispose();$shorts.Dispose();$hair.Dispose()
}

function Write-Utf8([string]$path, [string]$text) {
    [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false))
}

function Write-Json([string]$path, $value) {
    Write-Utf8 $path ($value | ConvertTo-Json -Depth 30)
}

# ---------------------------------------------------------------------------
# effect.survival-hazards.phase-silhouette-a
# ---------------------------------------------------------------------------
$hazards = @('injury','storm','theft')
$phases = @('telegraph','occurrence','mitigation','recovery')
$hazardAtlas = New-Canvas 1024 768 $true
for($row=0;$row -lt 3;$row++){
    for($col=0;$col -lt 4;$col++){
        Draw-Hazard $hazardAtlas.Graphics $hazards[$row] $phases[$col] ([System.Drawing.RectangleF]::new($col*256+30,$row*256+30,196,196))
    }
}
$hazardAtlasPath = Join-Path $hazardRoot 'hazard-phase-atlas.png'
Save-Png $hazardAtlas $hazardAtlasPath

$hazardSvg = @'
<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="768" viewBox="0 0 1024 768">
  <metadata>candidateId=effect.survival-hazards.phase-silhouette-a; cells=4x3; cell=256; pivot=0.5,0.5; status=review</metadata>
  <defs>
    <style>.o{fill:none;stroke:#17353B;stroke-width:14;stroke-linecap:round;stroke-linejoin:round}.t{fill:#168C84;stroke:#17353B;stroke-width:14}.c{fill:#F4E0AE;stroke:#17353B;stroke-width:14}.a{fill:#E8682A;stroke:#17353B;stroke-width:14}.y{fill:#F3BE4D;stroke:#17353B;stroke-width:12}.g{fill:#6D9A56;stroke:#17353B;stroke-width:14}.dash{fill:none;stroke:#F3BE4D;stroke-width:12;stroke-dasharray:22 14;stroke-linejoin:round}</style>
    <g id="phase-telegraph"><path class="dash" d="M128 34 222 204 34 204Z"/><path class="o" stroke="#F3BE4D" d="M38 88q20-35 50-45M216 88q-20-35-50-45"/></g>
    <g id="phase-occurrence"><path class="a" d="m128 24 18 34 34-22 3 42 42-3-22 34 34 18-34 18 22 34-42-3-3 42-34-22-18 34-18-34-34 22-3-42-42 3 22-34-34-18 34-18-22-34 42 3 3-42 34 22Z"/></g>
    <g id="phase-mitigation"><path class="t" d="M128 24 214 62 196 178 128 230 60 178 42 62Z"/></g>
    <g id="phase-recovery"><circle class="g" cx="128" cy="128" r="102"/><ellipse class="y" cx="180" cy="46" rx="20" ry="34"/></g>
    <g id="hazard-injury"><g transform="rotate(-38 128 128)"><rect class="c" x="58" y="102" width="140" height="52" rx="18"/><rect fill="#E8682A" x="104" y="98" width="48" height="60"/></g></g>
    <g id="hazard-storm"><path class="c" d="M58 140q-18-42 26-58 12-52 66-38 30 2 38 36 48 4 42 60Z"/><path class="o" d="m78 158-20 46m66-46-20 46m66-46-20 46"/></g>
    <g id="hazard-theft"><rect class="a" x="58" y="104" width="140" height="88" rx="12"/><path class="o" d="m70 116 116 64m0-64L70 180M54 98h148"/><circle fill="#17353B" cx="74" cy="64" r="10"/><circle fill="#17353B" cx="112" cy="48" r="10"/><circle fill="#17353B" cx="150" cy="64" r="10"/></g>
  </defs>
  <g id="hazard.injury.telegraph" transform="translate(0 0)"><use href="#phase-telegraph"/><use href="#hazard-injury"/></g>
  <g id="hazard.injury.occurrence" transform="translate(256 0)"><use href="#phase-occurrence"/><use href="#hazard-injury"/></g>
  <g id="hazard.injury.mitigation" transform="translate(512 0)"><use href="#phase-mitigation"/><use href="#hazard-injury"/></g>
  <g id="hazard.injury.recovery" transform="translate(768 0)"><use href="#phase-recovery"/><use href="#hazard-injury"/></g>
  <g id="hazard.disaster.telegraph" transform="translate(0 256)"><use href="#phase-telegraph"/><use href="#hazard-storm"/></g>
  <g id="hazard.disaster.occurrence" transform="translate(256 256)"><use href="#phase-occurrence"/><use href="#hazard-storm"/></g>
  <g id="hazard.disaster.mitigation" transform="translate(512 256)"><use href="#phase-mitigation"/><use href="#hazard-storm"/></g>
  <g id="hazard.disaster.recovery" transform="translate(768 256)"><use href="#phase-recovery"/><use href="#hazard-storm"/></g>
  <g id="hazard.food-theft.telegraph" transform="translate(0 512)"><use href="#phase-telegraph"/><use href="#hazard-theft"/></g>
  <g id="hazard.food-theft.occurrence" transform="translate(256 512)"><use href="#phase-occurrence"/><use href="#hazard-theft"/></g>
  <g id="hazard.food-theft.mitigation" transform="translate(512 512)"><use href="#phase-mitigation"/><use href="#hazard-theft"/></g>
  <g id="hazard.food-theft.recovery" transform="translate(768 512)"><use href="#phase-recovery"/><use href="#hazard-theft"/></g>
</svg>
'@
Write-Utf8 (Join-Path $hazardRoot 'hazard-phase-atlas.svg') $hazardSvg

$atlasImage = [System.Drawing.Image]::FromFile($hazardAtlasPath)
$read = New-Canvas 1024 512 $true
RoundedRect $read.Graphics ([System.Drawing.RectangleF]::new(18,18,988,476)) 24 '#102F38' '#4DB7BF' 4
Draw-Text $read.Graphics 'ACTUAL-SIZE READABILITY / 64 PX + 32 PX' 42 36 24 '#F4E0AE' $true
for($row=0;$row -lt 2;$row++){
    $size=if($row -eq 0){64}else{32};$y=108+$row*190
    Draw-Text $read.Graphics ("{0}px" -f $size) 46 ($y+18) 26 '#F3BE4D' $true
    for($i=0;$i -lt 12;$i++){
        $sx=($i%4)*256;$sy=[Math]::Floor($i/4)*256;$x=130+($i%6)*142;$yy=$y+[Math]::Floor($i/6)*78
        $read.Graphics.DrawImage($atlasImage,[System.Drawing.RectangleF]::new($x,$yy,$size,$size),[System.Drawing.RectangleF]::new($sx,$sy,256,256),[System.Drawing.GraphicsUnit]::Pixel)
    }
}
Save-Png $read (Join-Path $hazardRoot 'hazard-readability-32-64.png')

$hazardBoard = New-Canvas 1920 1080 $true
RoundedRect $hazardBoard.Graphics ([System.Drawing.RectangleF]::new(24,24,1872,1032)) 28 '#102F38' '#4DB7BF' 5
Draw-Text $hazardBoard.Graphics 'WAVE 16 / effect.survival-hazards.phase-silhouette-a / REVIEW ONLY' 58 46 30 '#F4E0AE' $true
for($col=0;$col -lt 4;$col++){Draw-Text $hazardBoard.Graphics $phases[$col].ToUpperInvariant() (340+$col*370) 112 24 '#F3BE4D' $true}
for($row=0;$row -lt 3;$row++){
    Draw-Text $hazardBoard.Graphics ("hazard.{0}" -f $(if($hazards[$row]-eq 'storm'){'disaster'}elseif($hazards[$row]-eq 'theft'){'food-theft'}else{'injury'})) 60 (230+$row*260) 24 '#4DB7BF' $true
    for($col=0;$col -lt 4;$col++){
        $x=300+$col*370;$y=170+$row*260
        RoundedRect $hazardBoard.Graphics ([System.Drawing.RectangleF]::new($x,$y,300,220)) 22 '#EAD8A8' '#17353B' 5
        $hazardBoard.Graphics.DrawImage($atlasImage,[System.Drawing.RectangleF]::new($x+70,$y+18,160,160),[System.Drawing.RectangleF]::new($col*256,$row*256,256,256),[System.Drawing.GraphicsUnit]::Pixel)
        Draw-SafeRect $hazardBoard.Graphics ([System.Drawing.RectangleF]::new($x+34,$y+176,188,28)) '#168C84'
        Draw-GlyphSlot $hazardBoard.Graphics ($x+238) ($y+166) 44
    }
}
Draw-Text $hazardBoard.Graphics 'SHAPE KEY: DASHED WARNING / JAGGED IMPACT / SHIELD CHEVRON / ROUND RECOVERY' 58 984 22 '#F4E0AE' $true
Save-Png $hazardBoard (Join-Path $hazardRoot 'hazard-review-board-1920x1080.png')
$atlasImage.Dispose()

$hazardManifest = [ordered]@{
    schemaVersion=1; assetId='effect.survival-hazards'; candidateId='effect.survival-hazards.phase-silhouette-a'; status='review'; selectedCandidate=$null
    scope=[ordered]@{ hazards=@('hazard.injury','hazard.disaster','hazard.food-theft'); phases=$phases; cells=12 }
    atlas=[ordered]@{ file='hazard-phase-atlas.png'; editable='hazard-phase-atlas.svg'; width=1024; height=768; columns=4; rows=3; cellWidth=256; cellHeight=256; padding=30; pivot=@{x=0.5;y=0.5}; ppu=100; trueAlpha=$true }
    phaseGrammar=[ordered]@{ telegraph='dashed triangle plus pulse arcs'; occurrence='jagged impact burst'; mitigation='shield silhouette plus chevron/check'; recovery='round field plus rising leaf/sun' }
    timingMs=[ordered]@{ telegraph='600-900 loop'; occurrence='160-220 one-shot'; mitigation='260-360 one-shot'; recovery='400-600 ease-out' }
    uiAttachment=[ordered]@{ minimumFocus=@{width=44;height=44}; glyphSlot=@{width=44;height=44}; locales=@('ko','en','qps-long'); rasterBodyText='none' }
    runtime=[ordered]@{ allowlist=@(); runtimeConnectAllowed=$false; packageAllowed=$false }
}
Write-Json (Join-Path $hazardRoot 'hazard-phase-manifest.json') $hazardManifest
Write-Json (Join-Path $hazardRoot 'hazard-visual-qa.json') ([ordered]@{candidateId='effect.survival-hazards.phase-silhouette-a';actualSizes=@(64,32);checks=@{trueAlpha=$true;edgePadding='>=30px per 256 cell';nonGraphic=$true;colorIndependent=$true;phaseSequence='telegraph>occurrence>mitigation>recovery';localizedRasterText=$false};manualReview=@('distinguish injury/storm/theft at 32px','confirm phase grammar without color','confirm comic tone is non-graphic');status='review'})
Write-Utf8 (Join-Path $hazardRoot 'hazard-handoff.txt') @'
effect.survival-hazards.phase-silhouette-a / REVIEW ONLY
Atlas: 1024x768, 4 columns x 3 rows, 256x256 cells, 30px internal visual padding, center pivot, PPU 100, true alpha.
Rows: injury, disaster (rain/storm), food-theft. Columns: telegraph, occurrence, mitigation, recovery.
Color-independent grammar: dashed warning, jagged impact, shield/chevron mitigation, round rising recovery.
No localized raster body text. UI attachment uses TMP and a minimum 44x44 focus/glyph slot.
selectedCandidate=null; runtime allowlist empty; adopt/package/runtime-connect prohibited until explicit user selection.
'@

# ---------------------------------------------------------------------------
# ui.escape-project-progress.route-signature-a
# ---------------------------------------------------------------------------
function Draw-EscapeCandidate([System.Drawing.Graphics]$g, [float]$scale = 1.0, [float]$ox = 0, [float]$oy = 0) {
    $state=$g.Save();$g.TranslateTransform($ox,$oy);$g.ScaleTransform($scale,$scale)
    $bg=Brush '#102F38';$g.FillRectangle($bg,0,0,1280,800);$bg.Dispose()
    RoundedRect $g ([System.Drawing.RectangleF]::new(36,38,1208,724)) 30 '#17353B' '#4DB7BF' 5
    $orange=Pen '#E8682A' 5;$g.DrawLine($orange,68,64,1212,64);$g.DrawLine($orange,68,736,1212,736);$orange.Dispose()
    RoundedRect $g ([System.Drawing.RectangleF]::new(68,86,1144,628)) 24 '#EAD8A8' '#17353B' 5
    RoundedRect $g ([System.Drawing.RectangleF]::new(88,108,250,584)) 20 '#133F48' '#4DB7BF' 4
    $routes=@('smoke','radio','raft','flare','beacon')
    for($i=0;$i -lt 5;$i++){
        $y=126+$i*108;$selected=($i -eq 0);$fill=if($selected){'#F3BE4D'}else{'#EAD8A8'};$stroke=if($selected){'#E8682A'}else{'#17353B'}
        $cardStrokeWidth = if($selected){6}else{4}
        $safeStroke = if($selected){'#17353B'}else{'#168C84'}
        RoundedRect $g ([System.Drawing.RectangleF]::new(106,$y,214,88)) 16 $fill $stroke $cardStrokeWidth
        Draw-RouteGlyph $g $routes[$i] ([System.Drawing.RectangleF]::new(118,$y+10,68,68))
        Draw-SafeRect $g ([System.Drawing.RectangleF]::new(196,$y+18,98,38)) $safeStroke
        if($i -lt 2){Draw-Check $g 278 ($y+62) 18 '#168C84'}else{Draw-Hatch $g ([System.Drawing.RectangleF]::new(276,$y+58,26,18)) '#17353B' 7 190}
    }
    RoundedRect $g ([System.Drawing.RectangleF]::new(360,108,830,584)) 22 '#F4E0AE' '#17353B' 5
    Draw-SafeRect $g ([System.Drawing.RectangleF]::new(390,132,500,54)) '#168C84'
    RoundedRect $g ([System.Drawing.RectangleF]::new(930,126,226,122)) 18 '#EAD8A8' '#E8682A' 5
    Draw-RouteGlyph $g 'smoke' ([System.Drawing.RectangleF]::new(982,130,112,112))
    # route-specific progress spine
    $line=Pen '#17353B' 7;$g.DrawLine($line,416,260,1108,260);$line.Dispose()
    for($i=0;$i -lt 5;$i++){
        $x=416+$i*173
        if($i -lt 3){$b=Brush '#168C84';$g.FillEllipse($b,$x-22,238,44,44);$b.Dispose();Draw-Check $g ($x-11) 246 22}
        elseif($i -eq 3){$pts=[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($x,234),[System.Drawing.PointF]::new($x+26,260),[System.Drawing.PointF]::new($x,286),[System.Drawing.PointF]::new($x-26,260));$b=Brush '#F3BE4D';$g.FillPolygon($b,$pts);$b.Dispose();$p=Pen '#17353B' 5;$g.DrawPolygon($p,$pts);$p.Dispose()}
        else{$p=Pen '#17353B' 5;$p.DashStyle=[System.Drawing.Drawing2D.DashStyle]::Dash;$g.DrawEllipse($p,$x-22,238,44,44);$p.Dispose()}
        Draw-SafeRect $g ([System.Drawing.RectangleF]::new($x-64,294,128,32)) '#168C84'
    }
    # preparation conditions
    for($i=0;$i -lt 3;$i++){
        $x=390+$i*190;RoundedRect $g ([System.Drawing.RectangleF]::new($x,352,172,92)) 15 '#EAD8A8' '#17353B' 4
        $g.DrawEllipse((Pen '#17353B' 4),$x+14,369,44,44);Draw-SafeRect $g ([System.Drawing.RectangleF]::new($x+68,365,84,48)) '#168C84'
    }
    # weather window and risk band
    RoundedRect $g ([System.Drawing.RectangleF]::new(390,466,470,104)) 16 '#D7E1D0' '#17353B' 4
    $cloud=Brush '#F4E0AE';$g.FillEllipse($cloud,410,490,72,42);$g.FillEllipse($cloud,446,480,82,54);$cloud.Dispose();$wp=Pen '#4A8FB8' 5;for($i=0;$i -lt 4;$i++){$g.DrawLine($wp,430+$i*30,536,418+$i*30,556)};$wp.Dispose()
    Draw-SafeRect $g ([System.Drawing.RectangleF]::new(548,486,282,56)) '#168C84'
    RoundedRect $g ([System.Drawing.RectangleF]::new(880,466,280,104)) 16 '#E8C6A0' '#C9483A' 4
    Draw-Burst $g 920 518 26 14 '#E8682A' '#17353B';Draw-SafeRect $g ([System.Drawing.RectangleF]::new(960,490,174,54)) '#C9483A'
    # actions and glyphs
    RoundedRect $g ([System.Drawing.RectangleF]::new(390,600,294,64)) 16 '#168C84' '#17353B' 4;Draw-SafeRect $g ([System.Drawing.RectangleF]::new(410,614,214,36)) '#F4E0AE';Draw-GlyphSlot $g 630 610 44
    RoundedRect $g ([System.Drawing.RectangleF]::new(704,600,244,64)) 16 '#EAD8A8' '#17353B' 4;Draw-SafeRect $g ([System.Drawing.RectangleF]::new(724,614,164,36)) '#168C84';Draw-GlyphSlot $g 894 610 44
    RoundedRect $g ([System.Drawing.RectangleF]::new(968,600,192,64)) 16 '#E8C6A0' '#17353B' 4;Draw-Hatch $g ([System.Drawing.RectangleF]::new(982,612,164,40)) '#17353B' 10 100
    $g.Restore($state)
}

$escapeCandidate=New-Canvas 1280 800 $false;Draw-EscapeCandidate $escapeCandidate.Graphics
Save-Png $escapeCandidate (Join-Path $escapeRoot 'escape-project-route-signature-a-1280x800.png')
$escapeSvg=@'
<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="800" viewBox="0 0 1280 800">
 <metadata>candidateId=ui.escape-project-progress.route-signature-a; status=review; selectedCandidate=null; runtimeAllowlist=[]</metadata>
 <defs><style>.n{fill:#102F38}.d{fill:#17353B}.p{fill:#F4E0AE;stroke:#17353B;stroke-width:5}.c{fill:#EAD8A8;stroke:#17353B;stroke-width:4}.s{fill:none;stroke:#168C84;stroke-width:2;stroke-dasharray:8 6}.o{fill:none;stroke:#E8682A;stroke-width:6}.i{fill:none;stroke:#17353B;stroke-width:7;stroke-linecap:round;stroke-linejoin:round}.h{fill:url(#h)}</style><pattern id="h" width="12" height="12" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><line x1="0" y1="0" x2="0" y2="12" stroke="#17353B" stroke-width="3" opacity=".35"/></pattern></defs>
 <rect class="n" width="1280" height="800"/><rect class="d" x="36" y="38" width="1208" height="724" rx="30" stroke="#4DB7BF" stroke-width="5"/><path class="o" d="M68 64h1144M68 736h1144"/>
 <rect class="p" x="68" y="86" width="1144" height="628" rx="24"/><g id="route-rail"><rect fill="#133F48" stroke="#4DB7BF" stroke-width="4" x="88" y="108" width="250" height="584" rx="20"/><g id="escape.smoke"><rect fill="#F3BE4D" stroke="#E8682A" stroke-width="6" x="106" y="126" width="214" height="88" rx="16"/><path class="i" d="M151 192v-28m-18-9q18-25 36 0m-22-24q15-26 30 0"/><rect class="s" x="196" y="144" width="98" height="38"/></g><g id="escape.radio"><rect class="c" x="106" y="234" width="214" height="88" rx="16"/><rect class="i" x="127" y="256" width="52" height="38" rx="5"/><path class="i" d="m132 276 9-8 10 15 10-13 8 6m-2-20 12-16"/><rect class="s" x="196" y="252" width="98" height="38"/></g><g id="escape.raft"><rect class="c" x="106" y="342" width="214" height="88" rx="16"/><path class="i" d="M126 393h58l-10 15h-38Zm29-39v38m2-35 24 31"/><rect class="s" x="196" y="360" width="98" height="38"/><rect class="h" x="276" y="400" width="26" height="18"/></g><g id="escape.flare"><rect class="c" x="106" y="450" width="214" height="88" rx="16"/><path class="i" d="m141 510 17-43m8 39 13-36"/><path fill="#F3BE4D" stroke="#17353B" stroke-width="5" d="m181 458 7 11 13-2-5 12 10 8-13 4-1 13-10-9-12 7 2-14-11-7 13-6Z"/><rect class="s" x="196" y="468" width="98" height="38"/><rect class="h" x="276" y="508" width="26" height="18"/></g><g id="escape.beacon"><rect class="c" x="106" y="558" width="214" height="88" rx="16"/><path class="i" d="m153 578-19 53m19-53 19 53m-30-19h22m-17-18h12"/><circle fill="#F3BE4D" stroke="#17353B" stroke-width="5" cx="153" cy="574" r="9"/><rect class="s" x="196" y="576" width="98" height="38"/><rect class="h" x="276" y="616" width="26" height="18"/></g></g>
 <g id="detail"><rect class="p" x="360" y="108" width="830" height="584" rx="22"/><rect class="s" x="390" y="132" width="500" height="54"/><rect class="c" x="930" y="126" width="226" height="122" rx="18" stroke="#E8682A" stroke-width="5"/><path class="i" d="M1042 221v-46m-30-10q30-38 60 0m-36-30q25-40 50 0"/>
 <path class="i" d="M416 260h692"/><g id="progress-stages"><circle fill="#168C84" cx="416" cy="260" r="22"/><circle fill="#168C84" cx="589" cy="260" r="22"/><circle fill="#168C84" cx="762" cy="260" r="22"/><path fill="#F3BE4D" stroke="#17353B" stroke-width="5" d="m935 234 26 26-26 26-26-26Z"/><circle fill="none" stroke="#17353B" stroke-width="5" stroke-dasharray="8 6" cx="1108" cy="260" r="22"/></g>
 <g id="tmp-safe-rects" class="s"><rect x="352" y="294" width="128" height="32"/><rect x="525" y="294" width="128" height="32"/><rect x="698" y="294" width="128" height="32"/><rect x="871" y="294" width="128" height="32"/><rect x="1044" y="294" width="128" height="32"/><rect x="458" y="365" width="84" height="48"/><rect x="648" y="365" width="84" height="48"/><rect x="838" y="365" width="84" height="48"/><rect x="548" y="486" width="282" height="56"/><rect x="960" y="490" width="174" height="54"/><rect x="410" y="614" width="214" height="36"/><rect x="724" y="614" width="164" height="36"/></g>
 <rect class="c" x="390" y="352" width="172" height="92" rx="15"/><rect class="c" x="580" y="352" width="172" height="92" rx="15"/><rect class="c" x="770" y="352" width="172" height="92" rx="15"/><rect fill="#D7E1D0" stroke="#17353B" stroke-width="4" x="390" y="466" width="470" height="104" rx="16"/><rect fill="#E8C6A0" stroke="#C9483A" stroke-width="4" x="880" y="466" width="280" height="104" rx="16"/><rect fill="#168C84" stroke="#17353B" stroke-width="4" x="390" y="600" width="294" height="64" rx="16"/><rect class="c" x="704" y="600" width="244" height="64" rx="16"/><rect class="h" x="968" y="600" width="192" height="64" rx="16"/></g>
</svg>
'@
Write-Utf8 (Join-Path $escapeRoot 'escape-project-route-signature-a.svg') $escapeSvg

$escapeBoard=New-Canvas 1920 1080 $false
Draw-Text $escapeBoard.Graphics 'WAVE 16 / ui.escape-project-progress.route-signature-a / REVIEW ONLY' 48 30 30 '#F4E0AE' $true
Draw-EscapeCandidate $escapeBoard.Graphics 0.66 44 92
RoundedRect $escapeBoard.Graphics ([System.Drawing.RectangleF]::new(920,92,952,680)) 26 '#EAD8A8' '#4DB7BF' 5
Draw-Text $escapeBoard.Graphics 'ROUTE SIGNATURES / NOT CLONED BUTTONS' 956 118 25 '#17353B' $true
$routes=@('smoke','radio','raft','flare','beacon')
for($i=0;$i -lt 5;$i++){
    $x=954+($i%3)*290;$y=176+[Math]::Floor($i/3)*250
    RoundedRect $escapeBoard.Graphics ([System.Drawing.RectangleF]::new($x,$y,256,214)) 18 '#F4E0AE' '#17353B' 4
    Draw-RouteGlyph $escapeBoard.Graphics $routes[$i] ([System.Drawing.RectangleF]::new($x+64,$y+18,128,128))
    Draw-Text $escapeBoard.Graphics ("escape.{0}" -f $routes[$i]) ($x+24) ($y+154) 20 '#17353B' $true
    Draw-Text $escapeBoard.Graphics $(if($i -lt 2){'PLAYABLE'}else{'DATA PATH'}) ($x+24) ($y+182) 17 $(if($i -lt 2){'#168C84'}else{'#C9483A'}) $true
}
RoundedRect $escapeBoard.Graphics ([System.Drawing.RectangleF]::new(44,808,1828,224)) 22 '#17353B' '#4DB7BF' 4
Draw-Text $escapeBoard.Graphics 'LOCALE / INPUT INVARIANTS' 76 834 24 '#F3BE4D' $true
Draw-Text $escapeBoard.Graphics 'ko / en / qps-long 150% : wrap -> vertical reflow; minimum 18 px' 76 878 22 '#F4E0AE'
Draw-Text $escapeBoard.Graphics '44x44 keyboard + gamepad glyph/focus slot; route state also uses silhouette + pattern' 76 918 22 '#F4E0AE'
Draw-Text $escapeBoard.Graphics 'Smoke + radio playable. Raft + flare + beacon remain data-only catalog paths.' 76 958 22 '#F4E0AE'
for($i=0;$i -lt 4;$i++){Draw-GlyphSlot $escapeBoard.Graphics (1460+$i*88) 882 56}
Save-Png $escapeBoard (Join-Path $escapeRoot 'escape-project-review-board-1920x1080.png')

$escapeManifest=[ordered]@{
    schemaVersion=1;assetId='ui.escape-project-progress';candidateId='ui.escape-project-progress.route-signature-a';status='review';selectedCandidate=$null
    canvas=@{width=1280;height=800};mode='situational facility popup only';playableRoutes=@('escape.smoke','escape.radio');dataOnlyRoutes=@('escape.raft','escape.flare','escape.beacon')
    routeGrammar=[ordered]@{smoke='vertical fuel rings + wind/rain';radio='circuit grid + frequency waveform + power chain';raft='horizontal hull planks + sea window arc';flare='single-shot chamber + timing wedge';beacon='vertical tower + structure/power checkpoints'}
    layout=[ordered]@{outerSafe=@{left=36;right=36;top=38;bottom=38};routeRail=@{x=88;y=108;width=250;height=584};detail=@{x=360;y=108;width=830;height=584};titleTmp=@{x=390;y=132;width=500;height=54};weather=@{x=390;y=466;width=470;height=104};risk=@{x=880;y=466;width=280;height=104};minimumFocus=@{width=44;height=44}}
    localization=@{rasterBodyText='none';tmpLocales=@('ko','en','qps-long');qpsLongExpansion=1.5;minimumTextPx=18;overflow='wrap then vertical reflow';glyphDevices=@('keyboard-mouse','gamepad')}
    slices=@{popup=@{left=32;right=32;top=28;bottom=28};routeCard=@{left=20;right=20;top=16;bottom=16};action=@{left=18;right=18;top=14;bottom=14}}
    runtime=@{allowlist=@();runtimeConnectAllowed=$false;packageAllowed=$false}
}
Write-Json (Join-Path $escapeRoot 'escape-project-manifest.json') $escapeManifest
Write-Json (Join-Path $escapeRoot 'escape-project-visual-qa.json') ([ordered]@{candidateId='ui.escape-project-progress.route-signature-a';checks=@{actualSize='1280x800';routeShapeDistinct=$true;playableVsDataOnly=$true;globalDashboard=$false;localizedRasterText=$false;qpsLongExpansion=1.5;focus='44x44';colorIndependent=$true};manualReview=@('compare smoke stacked fuel/wind silhouette against radio circuit/waveform','verify three data paths are readable but not presented as playable clones','verify panel remains situational and does not cover camp persistently');status='review'})
Write-Utf8 (Join-Path $escapeRoot 'escape-project-handoff.txt') @'
ui.escape-project-progress.route-signature-a / REVIEW ONLY
1280x800 situational facility popup. It is not a persistent global dashboard.
Playable: escape.smoke and escape.radio. Data-only catalog: escape.raft, escape.flare, escape.beacon.
Route identity is silhouette-led: fuel/wind, circuit/waveform, hull/sea arc, single-shot/timing wedge, tower/checkpoints.
TMP only for ko/en/qps-long. qps-long expansion 150%, wrap then vertical reflow, minimum 18px. Focus/glyph minimum 44x44.
9-slice: popup L32 R32 T28 B28; route card L20 R20 T16 B16; action L18 R18 T14 B14.
selectedCandidate=null; runtime allowlist empty; adopt/package/runtime-connect prohibited until explicit user selection.
'@

# ---------------------------------------------------------------------------
# ui.ending-comic.triptych-a
# ---------------------------------------------------------------------------
function Draw-EndingCandidate([System.Drawing.Graphics]$g,[float]$scale=1.0,[float]$ox=0,[float]$oy=0){
    $state=$g.Save();$g.TranslateTransform($ox,$oy);$g.ScaleTransform($scale,$scale)
    $bg=Brush '#102F38';$g.FillRectangle($bg,0,0,1280,800);$bg.Dispose()
    RoundedRect $g ([System.Drawing.RectangleF]::new(36,38,1208,724)) 30 '#17353B' '#4DB7BF' 5
    $orange=Pen '#E8682A' 5;$g.DrawLine($orange,68,64,1212,64);$orange.Dispose()
    Draw-SafeRect $g ([System.Drawing.RectangleF]::new(82,84,820,50)) '#F3BE4D';Draw-GlyphSlot $g 1130 82 44
    # panel 1: setup / smoke preparation
    $p1=[System.Drawing.RectangleF]::new(64,154,500,344);RoundedRect $g $p1 18 '#EAD8A8' '#F4E0AE' 8
    $sky=Brush '#4DB7BF' 70;$g.FillRectangle($sky,78,168,472,202);$sky.Dispose();Draw-CharacterPlaceholder $g 120 226 0.72 'stand'
    $smokePen=Pen '#687B78' 12 190;$g.DrawArc($smokePen,300,196,120,150,150,120);$g.DrawArc($smokePen,346,164,110,150,145,120);$smokePen.Dispose()
    Draw-SafeRect $g ([System.Drawing.RectangleF]::new(92,442,444,36)) '#168C84'
    # panel 2: close-up / radio response
    $pts2=[System.Drawing.PointF[]]@([System.Drawing.PointF]::new(592,144),[System.Drawing.PointF]::new(866,164),[System.Drawing.PointF]::new(842,506),[System.Drawing.PointF]::new(606,494));$fill=Brush '#F4E0AE';$g.FillPolygon($fill,$pts2);$fill.Dispose();$p=Pen '#E8682A' 8;$g.DrawPolygon($p,$pts2);$p.Dispose()
    Draw-CharacterPlaceholder $g 630 204 0.78 'radio'
    $wave=Pen '#168C84' 7;$g.DrawArc($wave,744,188,72,72,280,120);$g.DrawArc($wave,724,168,112,112,285,110);$wave.Dispose()
    Draw-SafeRect $g ([System.Drawing.RectangleF]::new(626,450,188,30)) '#168C84'
    # panel 3: punchline/result
    $p3=[System.Drawing.RectangleF]::new(888,154,328,344);RoundedRect $g $p3 18 '#D9C58F' '#F3BE4D' 8
    $sun=Brush '#F3BE4D';$g.FillEllipse($sun,1072,186,82,82);$sun.Dispose();$sea=Pen '#168C84' 9;for($i=0;$i -lt 4;$i++){$g.DrawArc($sea,916,302+$i*30,270,80,190,160)};$sea.Dispose()
    $boat=Pen '#17353B' 8;$g.DrawLines($boat,[System.Drawing.PointF[]]@([System.Drawing.PointF]::new(972,312),[System.Drawing.PointF]::new(1134,312),[System.Drawing.PointF]::new(1108,348),[System.Drawing.PointF]::new(996,348),[System.Drawing.PointF]::new(972,312)));$g.DrawLine($boat,1046,310,1046,248);$g.DrawLine($boat,1046,250,1100,304);$boat.Dispose()
    Draw-SafeRect $g ([System.Drawing.RectangleF]::new(914,442,276,36)) '#168C84'
    # transition arrows and optional inserts
    for($x=566;$x -le 866;$x+=300){$b=Brush '#E8682A';$tri=[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($x+8,312),[System.Drawing.PointF]::new($x+28,326),[System.Drawing.PointF]::new($x+8,340));$g.FillPolygon($b,$tri);$b.Dispose()}
    RoundedRect $g ([System.Drawing.RectangleF]::new(84,518,300,32)) 8 '#133F48' '#4DB7BF' 3;Draw-SafeRect $g ([System.Drawing.RectangleF]::new(98,524,272,20)) '#4DB7BF'
    RoundedRect $g ([System.Drawing.RectangleF]::new(396,518,300,32)) 8 '#133F48' '#E8682A' 3;Draw-SafeRect $g ([System.Drawing.RectangleF]::new(410,524,272,20)) '#E8682A'
    # verdict/evidence band
    RoundedRect $g ([System.Drawing.RectangleF]::new(64,572,1152,142)) 18 '#EAD8A8' '#17353B' 5
    for($i=0;$i -lt 4;$i++){
        $x=84+$i*230;RoundedRect $g ([System.Drawing.RectangleF]::new($x,594,210,72)) 14 '#F4E0AE' '#17353B' 3
        $mark=if($i -eq 0){'#E8682A'}elseif($i -eq 1){'#168C84'}elseif($i -eq 2){'#F3BE4D'}else{'#687B78'};$b=Brush $mark;$g.FillEllipse($b,$x+14,608,40,40);$b.Dispose();Draw-SafeRect $g ([System.Drawing.RectangleF]::new($x+66,606,126,42)) '#168C84'
    }
    RoundedRect $g ([System.Drawing.RectangleF]::new(1018,590,172,82)) 14 '#168C84' '#17353B' 4;Draw-SafeRect $g ([System.Drawing.RectangleF]::new(1034,606,98,42)) '#F4E0AE';Draw-GlyphSlot $g 1138 609 44
    $g.Restore($state)
}

$endingCandidate=New-Canvas 1280 800 $false;Draw-EndingCandidate $endingCandidate.Graphics
Save-Png $endingCandidate (Join-Path $endingRoot 'ending-comic-triptych-a-1280x800.png')
$endingSvg=@'
<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="800" viewBox="0 0 1280 800">
 <metadata>candidateId=ui.ending-comic.triptych-a; status=review; selectedCandidate=null; runtimeAllowlist=[]</metadata>
 <defs><style>.n{fill:#102F38}.d{fill:#17353B}.p{fill:#F4E0AE;stroke:#17353B;stroke-width:5}.c{fill:#EAD8A8;stroke:#17353B;stroke-width:5}.s{fill:none;stroke:#168C84;stroke-width:2;stroke-dasharray:8 6}.o{fill:none;stroke:#E8682A;stroke-width:8;stroke-linecap:round;stroke-linejoin:round}.i{fill:none;stroke:#17353B;stroke-width:7;stroke-linecap:round;stroke-linejoin:round}</style></defs>
 <rect class="n" width="1280" height="800"/><rect class="d" x="36" y="38" width="1208" height="724" rx="30" stroke="#4DB7BF" stroke-width="5"/><path class="o" d="M68 64h1144"/><rect class="s" x="82" y="84" width="820" height="50"/><rect fill="#F3BE4D" stroke="#17353B" stroke-width="4" x="1130" y="82" width="44" height="44" rx="10"/>
 <g id="panel.setup"><rect class="c" x="64" y="154" width="500" height="344" rx="18" stroke="#F4E0AE" stroke-width="8"/><rect fill="#4DB7BF" opacity=".28" x="78" y="168" width="472" height="202"/><g id="mr-kim-placeholder"><circle fill="#E8A06B" stroke="#17353B" stroke-width="5" cx="184" cy="244" r="27"/><rect fill="#E8682A" stroke="#17353B" stroke-width="5" x="154" y="270" width="72" height="96" rx="14"/><rect fill="#526F45" stroke="#17353B" stroke-width="5" x="158" y="354" width="64" height="42"/></g><path fill="none" stroke="#687B78" stroke-width="12" stroke-linecap="round" d="M302 330q110-60 50-140m56 118q100-70 30-160"/><rect class="s" x="92" y="442" width="444" height="36"/></g>
 <g id="panel.closeup"><path class="p" stroke="#E8682A" stroke-width="8" d="m592 144 274 20-24 342-236-12Z"/><circle fill="#E8A06B" stroke="#17353B" stroke-width="6" cx="690" cy="260" r="56"/><path fill="#1D2426" d="M630 252q16-75 88-50 36 10 28 62-48-26-116-12Z"/><rect fill="#E8682A" stroke="#17353B" stroke-width="6" x="630" y="316" width="140" height="112" rx="20"/><path class="o" stroke="#168C84" d="M752 236q46-34 80 4m-94-34q70-58 116 10"/><rect class="s" x="626" y="450" width="188" height="30"/></g>
 <g id="panel.result"><rect fill="#D9C58F" stroke="#F3BE4D" stroke-width="8" x="888" y="154" width="328" height="344" rx="18"/><circle fill="#F3BE4D" cx="1113" cy="227" r="41"/><path fill="none" stroke="#168C84" stroke-width="9" d="M916 332q120 50 270 0m-270 30q120 50 270 0m-270 30q120 50 270 0"/><path class="i" d="m972 312 162 0-26 36H996Zm74-2v-62l54 56"/><rect class="s" x="914" y="442" width="276" height="36"/></g>
 <g id="modifier-slots"><rect fill="#133F48" stroke="#4DB7BF" stroke-width="3" x="84" y="518" width="300" height="32" rx="8"/><rect class="s" x="98" y="524" width="272" height="20"/><rect fill="#133F48" stroke="#E8682A" stroke-width="3" x="396" y="518" width="300" height="32" rx="8"/><rect class="s" x="410" y="524" width="272" height="20"/></g>
 <g id="verdict-evidence"><rect class="c" x="64" y="572" width="1152" height="142" rx="18"/><g class="s"><rect x="150" y="606" width="126" height="42"/><rect x="380" y="606" width="126" height="42"/><rect x="610" y="606" width="126" height="42"/><rect x="840" y="606" width="126" height="42"/><rect x="1034" y="606" width="98" height="42"/></g><circle fill="#E8682A" cx="118" cy="628" r="20"/><circle fill="#168C84" cx="348" cy="628" r="20"/><circle fill="#F3BE4D" cx="578" cy="628" r="20"/><circle fill="#687B78" cx="808" cy="628" r="20"/><rect fill="#168C84" stroke="#17353B" stroke-width="4" x="1018" y="590" width="172" height="82" rx="14"/></g>
 </svg>
'@
Write-Utf8 (Join-Path $endingRoot 'ending-comic-triptych-a.svg') $endingSvg

$endingBoard=New-Canvas 1920 1080 $false
Draw-Text $endingBoard.Graphics 'WAVE 16 / ui.ending-comic.triptych-a / REVIEW ONLY' 48 30 30 '#F4E0AE' $true
Draw-EndingCandidate $endingBoard.Graphics 0.70 40 88
RoundedRect $endingBoard.Graphics ([System.Drawing.RectangleF]::new(970,92,902,568)) 26 '#EAD8A8' '#4DB7BF' 5
Draw-Text $endingBoard.Graphics 'CATEGORY FRAME GRAMMAR / COLOR-INDEPENDENT' 1002 118 24 '#17353B' $true
$cats=@('NORMAL','COMIC','RARE','DAY50')
for($i=0;$i -lt 4;$i++){
    $x=1004+($i%2)*420;$y=176+[Math]::Floor($i/2)*220
    $stroke=if($i-eq0){'#168C84'}elseif($i-eq1){'#E8682A'}elseif($i-eq2){'#F3BE4D'}else{'#687B78'}
    if($i-eq1){Draw-Burst $endingBoard.Graphics ($x+174) ($y+86) 96 80 '#F4E0AE' $stroke}
    elseif($i-eq2){$star=[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($x+174,$y+4),[System.Drawing.PointF]::new($x+202,$y+46),[System.Drawing.PointF]::new($x+250,$y+50),[System.Drawing.PointF]::new($x+218,$y+88),[System.Drawing.PointF]::new($x+230,$y+136),[System.Drawing.PointF]::new($x+174,$y+112),[System.Drawing.PointF]::new($x+118,$y+136),[System.Drawing.PointF]::new($x+130,$y+88),[System.Drawing.PointF]::new($x+98,$y+50),[System.Drawing.PointF]::new($x+146,$y+46));$b=Brush '#F4E0AE';$endingBoard.Graphics.FillPolygon($b,$star);$b.Dispose();$p=Pen $stroke 7;$endingBoard.Graphics.DrawPolygon($p,$star);$p.Dispose()}
    else{RoundedRect $endingBoard.Graphics ([System.Drawing.RectangleF]::new($x,$y,348,142)) $(if($i-eq0){22}else{8}) '#F4E0AE' $stroke 7;if($i-eq3){Draw-Hatch $endingBoard.Graphics ([System.Drawing.RectangleF]::new($x+10,$y+10,328,122)) $stroke 14 120}}
    Draw-Text $endingBoard.Graphics $cats[$i] ($x+18) ($y+154) 20 '#17353B' $true
}
RoundedRect $endingBoard.Graphics ([System.Drawing.RectangleF]::new(44,704,1828,328)) 22 '#17353B' '#4DB7BF' 4
Draw-Text $endingBoard.Graphics 'SAMPLE ENDING COVERAGE + TRANSITION CONTRACT' 76 730 24 '#F3BE4D' $true
$sampleIds=@('ending.escape.smoke.seen-from-afar','ending.escape.radio.clear-signal','ending.comic.radio.island-dj','ending.stay.just-kim')
for($i=0;$i -lt 4;$i++){Draw-Text $endingBoard.Graphics $sampleIds[$i] 76 (780+$i*48) 20 '#F4E0AE';Draw-SafeRect $endingBoard.Graphics ([System.Drawing.RectangleF]::new(590,778+$i*48,360,30)) '#168C84'}
Draw-Text $endingBoard.Graphics '3 core panels + <=1 dominant modifier + <=1 event scar' 1040 790 21 '#F4E0AE'
Draw-Text $endingBoard.Graphics 'TMP ko/en/qps-long 150%; minimum 18px; no raster body copy' 1040 838 21 '#F4E0AE'
Draw-Text $endingBoard.Graphics '44x44 keyboard/gamepad focus + glyph; verdict reason remains separate' 1040 886 21 '#F4E0AE'
for($i=0;$i-lt4;$i++){Draw-GlyphSlot $endingBoard.Graphics (1110+$i*90) 940 56}
Save-Png $endingBoard (Join-Path $endingRoot 'ending-comic-review-board-1920x1080.png')

$endingManifest=[ordered]@{
    schemaVersion=1;assetId='ui.ending-comic';candidateId='ui.ending-comic.triptych-a';status='review';selectedCandidate=$null
    canvas=@{width=1280;height=800};corePanels=@{count=3;setup=@{x=64;y=154;width=500;height=344};closeup='polygon bounds 592,144 to 866,506';result=@{x=888;y=154;width=328;height=344}}
    optionalInsertPolicy=@{dominantBehaviorMax=1;eventScarMax=1;endingIdNeverChanges=$true};representativeEndings=@('ending.escape.smoke.seen-from-afar','ending.escape.radio.clear-signal','ending.comic.radio.island-dj','ending.stay.just-kim')
    categoryGrammar=@{normal='rounded frame + double-line corner';comic='burst-cut frame + dot/punch marks';rare='star-cut frame + ray notches';day50='squared stitched frame + diagonal hatch'}
    verdictEvidence=@{x=64;y=572;width=1152;height=142;fields=@('terminal route','dominant behavior','special event','tie-break reason')}
    localization=@{rasterBodyText='none';tmpLocales=@('ko','en','qps-long');qpsLongExpansion=1.5;minimumTextPx=18;overflow='wrap then vertical reflow';minimumFocus=@{width=44;height=44};glyphDevices=@('keyboard-mouse','gamepad')}
    slices=@{outerFrame=@{left=32;right=32;top=28;bottom=28};comicPanel=@{left=16;right=16;top=16;bottom=16};evidenceBand=@{left=18;right=18;top=14;bottom=14}}
    runtime=@{allowlist=@();runtimeConnectAllowed=$false;packageAllowed=$false}
}
Write-Json (Join-Path $endingRoot 'ending-comic-manifest.json') $endingManifest
Write-Json (Join-Path $endingRoot 'ending-comic-visual-qa.json') ([ordered]@{candidateId='ui.ending-comic.triptych-a';checks=@{actualSize='1280x800';corePanels=3;optionalModifierMax=1;optionalScarMax=1;categoryShapeDistinct=$true;verdictEvidenceSeparated=$true;localizedRasterText=$false;qpsLongExpansion=1.5;focus='44x44'};manualReview=@('read setup-closeup-punchline sequence at 1280x800','verify verdict evidence is secondary but inspectable','verify normal/comic/rare/day50 frames differ without color');status='review'})
Write-Utf8 (Join-Path $endingRoot 'ending-comic-handoff.txt') @'
ui.ending-comic.triptych-a / REVIEW ONLY
1280x800 three-core-panel placeholder: setup, expressive close-up, punchline/result.
Optional inserts: maximum one dominant-behavior panel and one event-scar panel; modifiers never change endingId.
Verdict evidence remains a separate band for terminal route, dominant behavior, special event and deterministic tie-break reason.
Category shape grammar: rounded/double-line normal, burst/dot comic, star/ray rare, stitched/hatch Day50.
TMP only for ko/en/qps-long. qps-long expansion 150%, wrap then vertical reflow, minimum 18px. Focus/glyph minimum 44x44.
9-slice: outer L32 R32 T28 B28; panel L16 R16 T16 B16; evidence L18 R18 T14 B14.
selectedCandidate=null; runtime allowlist empty; adopt/package/runtime-connect prohibited until explicit user selection.
'@

Write-Output ([ordered]@{
    hazards=$hazardRoot
    escape=$escapeRoot
    ending=$endingRoot
    candidates=@('effect.survival-hazards.phase-silhouette-a','ui.escape-project-progress.route-signature-a','ui.ending-comic.triptych-a')
} | ConvertTo-Json -Depth 5)
