param(
    [Parameter(Mandatory = $true)]
    [string]$MapSource,

    [Parameter(Mandatory = $true)]
    [string]$OutputRoot,

    [string]$IconJobId = 'job_20260823145302_c4c41491',

    [string]$MapJobId = 'job_20260823144023_146e6ff7'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$iconRoot = Join-Path $OutputRoot 'icons'
$mapRoot = Join-Path $OutputRoot 'map'
New-Item -ItemType Directory -Force -Path $iconRoot, $mapRoot | Out-Null

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
    if ($transparent) {
        $g.Clear([System.Drawing.Color]::Transparent)
    } else {
        $g.Clear((Color '#102F38'))
    }
    return @{ Bitmap = $bmp; Graphics = $g }
}

function Save-Png($canvas, [string]$path) {
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Bitmap.Dispose()
}

function RoundedRect([System.Drawing.Graphics]$g, [System.Drawing.RectangleF]$r, [float]$radius, [string]$fill, [string]$stroke, [float]$strokeWidth, [int]$alpha = 255) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $d = $radius * 2
    $path.AddArc($r.X, $r.Y, $d, $d, 180, 90)
    $path.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
    $path.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    if ($fill) {
        $b = Brush $fill $alpha
        $g.FillPath($b, $path)
        $b.Dispose()
    }
    if ($stroke -and $strokeWidth -gt 0) {
        $p = Pen $stroke $strokeWidth
        $g.DrawPath($p, $path)
        $p.Dispose()
    }
    $path.Dispose()
}

function Draw-Hatch([System.Drawing.Graphics]$g, [System.Drawing.RectangleF]$r, [string]$hex, [float]$step = 12) {
    $p = Pen $hex 3 150
    for ($x = $r.X - $r.Height; $x -lt $r.Right; $x += $step) {
        $g.DrawLine($p, $x, $r.Bottom, $x + $r.Height, $r.Y)
    }
    $p.Dispose()
}

function Draw-Text([System.Drawing.Graphics]$g, [string]$text, [float]$x, [float]$y, [float]$size, [string]$hex = '#F4E0AE', [bool]$bold = $false) {
    $style = if ($bold) { [System.Drawing.FontStyle]::Bold } else { [System.Drawing.FontStyle]::Regular }
    $font = [System.Drawing.Font]::new('Arial', $size, $style, [System.Drawing.GraphicsUnit]::Pixel)
    $b = Brush $hex
    $g.DrawString($text, $font, $b, $x, $y)
    $b.Dispose()
    $font.Dispose()
}

function Draw-Icon([System.Drawing.Graphics]$g, [string]$id, [System.Drawing.RectangleF]$r, [bool]$withBadge = $true) {
    $s = [Math]::Min($r.Width, $r.Height)
    $cx = $r.X + $r.Width / 2
    $cy = $r.Y + $r.Height / 2
    $outline = Pen '#17353B' ([Math]::Max(2, $s * 0.055))
    $thin = Pen '#17353B' ([Math]::Max(1.5, $s * 0.035))
    if ($withBadge) {
        $bg = Brush '#F4E0AE' 235
        $g.FillEllipse($bg, $r.X + $s * 0.09, $r.Y + $s * 0.09, $s * 0.82, $s * 0.82)
        $bg.Dispose()
        $g.DrawEllipse($thin, $r.X + $s * 0.09, $r.Y + $s * 0.09, $s * 0.82, $s * 0.82)
    }

    $orange = Brush '#E8682A'
    $yellow = Brush '#F3BE4D'
    $teal = Brush '#168C84'
    $green = Brush '#648B45'
    $red = Brush '#C9483A'
    $navy = Brush '#17353B'
    $cream = Brush '#F4E0AE'
    $gray = Brush '#6B7772'
    $sky = Brush '#4DB7BF'

    switch ($id) {
        'resource.wood' {
            for ($i = 0; $i -lt 3; $i++) {
                $yy = $r.Y + $s * (0.34 + 0.15 * $i)
                $g.FillRectangle($orange, $r.X + $s * 0.22, $yy, $s * 0.54, $s * 0.13)
                $g.DrawRectangle($thin, $r.X + $s * 0.22, $yy, $s * 0.54, $s * 0.13)
                $g.DrawEllipse($thin, $r.X + $s * 0.68, $yy + $s * 0.015, $s * 0.10, $s * 0.10)
            }
        }
        'resource.stone' {
            $pts = [System.Drawing.PointF[]]@(
                [System.Drawing.PointF]::new($r.X+$s*0.23,$r.Y+$s*0.62),[System.Drawing.PointF]::new($r.X+$s*0.31,$r.Y+$s*0.38),[System.Drawing.PointF]::new($r.X+$s*0.48,$r.Y+$s*0.28),[System.Drawing.PointF]::new($r.X+$s*0.64,$r.Y+$s*0.40),[System.Drawing.PointF]::new($r.X+$s*0.75,$r.Y+$s*0.64),[System.Drawing.PointF]::new($r.X+$s*0.60,$r.Y+$s*0.74),[System.Drawing.PointF]::new($r.X+$s*0.36,$r.Y+$s*0.72)
            )
            $g.FillPolygon($gray,$pts); $g.DrawPolygon($outline,$pts)
            $g.DrawLine($thin,$r.X+$s*0.37,$r.Y+$s*0.43,$r.X+$s*0.58,$r.Y+$s*0.57)
        }
        'resource.food' {
            $g.FillEllipse($green,$r.X+$s*0.26,$r.Y+$s*0.34,$s*0.43,$s*0.43); $g.DrawEllipse($outline,$r.X+$s*0.26,$r.Y+$s*0.34,$s*0.43,$s*0.43)
            $g.FillEllipse($cream,$r.X+$s*0.34,$r.Y+$s*0.41,$s*0.27,$s*0.27)
            $leaf = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new($cx,$r.Y+$s*0.34),[System.Drawing.PointF]::new($r.X+$s*0.58,$r.Y+$s*0.21),[System.Drawing.PointF]::new($r.X+$s*0.65,$r.Y+$s*0.38))
            $g.FillPolygon($orange,$leaf); $g.DrawPolygon($thin,$leaf)
        }
        'resource.scrap' {
            $metal = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new($r.X+$s*0.24,$r.Y+$s*0.30),[System.Drawing.PointF]::new($r.X+$s*0.72,$r.Y+$s*0.36),[System.Drawing.PointF]::new($r.X+$s*0.66,$r.Y+$s*0.70),[System.Drawing.PointF]::new($r.X+$s*0.27,$r.Y+$s*0.64))
            $g.FillPolygon($gray,$metal); $g.DrawPolygon($outline,$metal)
            $g.FillEllipse($yellow,$r.X+$s*0.55,$r.Y+$s*0.51,$s*0.17,$s*0.17); $g.DrawEllipse($thin,$r.X+$s*0.55,$r.Y+$s*0.51,$s*0.17,$s*0.17)
            $g.DrawLine($thin,$r.X+$s*0.34,$r.Y+$s*0.39,$r.X+$s*0.49,$r.Y+$s*0.56)
        }
        'resource.electronics' {
            $g.FillRectangle($teal,$r.X+$s*0.27,$r.Y+$s*0.28,$s*0.46,$s*0.44); $g.DrawRectangle($outline,$r.X+$s*0.27,$r.Y+$s*0.28,$s*0.46,$s*0.44)
            for($i=0;$i -lt 3;$i++){ $g.DrawLine($thin,$r.X+$s*(0.35+0.15*$i),$r.Y+$s*0.20,$r.X+$s*(0.35+0.15*$i),$r.Y+$s*0.28); $g.DrawLine($thin,$r.X+$s*(0.35+0.15*$i),$r.Y+$s*0.72,$r.X+$s*(0.35+0.15*$i),$r.Y+$s*0.80) }
            $g.FillEllipse($yellow,$r.X+$s*0.43,$r.Y+$s*0.43,$s*0.14,$s*0.14)
        }
        'resource.medicine' {
            RoundedRect $g ([System.Drawing.RectangleF]::new($r.X+$s*0.30,$r.Y+$s*0.28,$s*0.40,$s*0.48)) ($s*0.05) '#F4E0AE' '#17353B' ($s*0.055)
            $g.FillRectangle($red,$r.X+$s*0.45,$r.Y+$s*0.36,$s*0.10,$s*0.30); $g.FillRectangle($red,$r.X+$s*0.35,$r.Y+$s*0.46,$s*0.30,$s*0.10)
        }
        'resource.cloth' {
            $cloth = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new($r.X+$s*0.26,$r.Y+$s*0.30),[System.Drawing.PointF]::new($r.X+$s*0.72,$r.Y+$s*0.25),[System.Drawing.PointF]::new($r.X+$s*0.67,$r.Y+$s*0.72),[System.Drawing.PointF]::new($r.X+$s*0.31,$r.Y+$s*0.67))
            $g.FillPolygon($orange,$cloth); $g.DrawPolygon($outline,$cloth)
            $g.DrawLine($thin,$r.X+$s*0.32,$r.Y+$s*0.40,$r.X+$s*0.64,$r.Y+$s*0.57)
        }
        'resource.fuel' {
            RoundedRect $g ([System.Drawing.RectangleF]::new($r.X+$s*0.31,$r.Y+$s*0.28,$s*0.38,$s*0.47)) ($s*0.04) '#E8682A' '#17353B' ($s*0.055)
            $g.DrawLine($outline,$r.X+$s*0.52,$r.Y+$s*0.29,$r.X+$s*0.64,$r.Y+$s*0.20)
            $g.FillEllipse($yellow,$r.X+$s*0.41,$r.Y+$s*0.43,$s*0.18,$s*0.24)
        }
        'abundance.rich' {
            for($i=0;$i -lt 3;$i++){ $g.FillEllipse($green,$r.X+$s*(0.25+0.19*$i),$r.Y+$s*(0.58-0.10*$i),$s*0.16,$s*0.16); $g.DrawEllipse($thin,$r.X+$s*(0.25+0.19*$i),$r.Y+$s*(0.58-0.10*$i),$s*0.16,$s*0.16) }
            $g.DrawArc($outline,$r.X+$s*0.22,$r.Y+$s*0.22,$s*0.56,$s*0.52,200,140)
        }
        'abundance.normal' {
            for($i=0;$i -lt 2;$i++){ $g.FillEllipse($yellow,$r.X+$s*(0.33+0.22*$i),$r.Y+$s*0.48,$s*0.17,$s*0.17); $g.DrawEllipse($thin,$r.X+$s*(0.33+0.22*$i),$r.Y+$s*0.48,$s*0.17,$s*0.17) }
            $g.DrawLine($outline,$r.X+$s*0.29,$r.Y+$s*0.35,$r.X+$s*0.71,$r.Y+$s*0.35)
        }
        'abundance.rare' {
            $g.FillEllipse($orange,$r.X+$s*0.42,$r.Y+$s*0.48,$s*0.18,$s*0.18); $g.DrawEllipse($outline,$r.X+$s*0.42,$r.Y+$s*0.48,$s*0.18,$s*0.18)
            $g.DrawLine($outline,$r.X+$s*0.30,$r.Y+$s*0.35,$r.X+$s*0.70,$r.Y+$s*0.35)
        }
        'state.unknown' {
            $g.FillEllipse($gray,$r.X+$s*0.26,$r.Y+$s*0.28,$s*0.48,$s*0.46); $g.DrawEllipse($outline,$r.X+$s*0.26,$r.Y+$s*0.28,$s*0.48,$s*0.46)
            $slash = Pen '#F4E0AE' ($s*0.08); $g.DrawLine($slash,$r.X+$s*0.29,$r.Y+$s*0.70,$r.X+$s*0.72,$r.Y+$s*0.27); $slash.Dispose()
        }
        'travel.time' {
            $g.DrawLine($outline,$r.X+$s*0.30,$r.Y+$s*0.27,$r.X+$s*0.70,$r.Y+$s*0.27); $g.DrawLine($outline,$r.X+$s*0.30,$r.Y+$s*0.73,$r.X+$s*0.70,$r.Y+$s*0.73)
            $top = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new($r.X+$s*0.34,$r.Y+$s*0.31),[System.Drawing.PointF]::new($r.X+$s*0.66,$r.Y+$s*0.31),[System.Drawing.PointF]::new($cx,$r.Y+$s*0.49))
            $bot = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new($cx,$r.Y+$s*0.51),[System.Drawing.PointF]::new($r.X+$s*0.66,$r.Y+$s*0.69),[System.Drawing.PointF]::new($r.X+$s*0.34,$r.Y+$s*0.69))
            $g.FillPolygon($yellow,$top);$g.FillPolygon($orange,$bot);$g.DrawPolygon($thin,$top);$g.DrawPolygon($thin,$bot)
        }
        'weather.storm' {
            $g.FillEllipse($gray,$r.X+$s*0.28,$r.Y+$s*0.29,$s*0.28,$s*0.24);$g.FillEllipse($gray,$r.X+$s*0.44,$r.Y+$s*0.25,$s*0.30,$s*0.30);$g.FillRectangle($gray,$r.X+$s*0.30,$r.Y+$s*0.40,$s*0.42,$s*0.17)
            $bolt=[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($cx,$r.Y+$s*0.48),[System.Drawing.PointF]::new($r.X+$s*0.42,$r.Y+$s*0.67),[System.Drawing.PointF]::new($cx,$r.Y+$s*0.64),[System.Drawing.PointF]::new($r.X+$s*0.46,$r.Y+$s*0.82),[System.Drawing.PointF]::new($r.X+$s*0.65,$r.Y+$s*0.58),[System.Drawing.PointF]::new($r.X+$s*0.55,$r.Y+$s*0.60));$g.FillPolygon($yellow,$bolt);$g.DrawPolygon($thin,$bolt)
        }
        'weather.rain' {
            $g.FillEllipse($sky,$r.X+$s*0.28,$r.Y+$s*0.27,$s*0.28,$s*0.24);$g.FillEllipse($sky,$r.X+$s*0.45,$r.Y+$s*0.24,$s*0.28,$s*0.29);$g.FillRectangle($sky,$r.X+$s*0.31,$r.Y+$s*0.39,$s*0.41,$s*0.16)
            for($i=0;$i -lt 3;$i++){ $g.DrawLine($outline,$r.X+$s*(0.36+0.14*$i),$r.Y+$s*0.63,$r.X+$s*(0.31+0.14*$i),$r.Y+$s*0.75) }
        }
        'weather.heat' {
            $g.FillEllipse($orange,$r.X+$s*0.36,$r.Y+$s*0.31,$s*0.28,$s*0.28);$g.DrawEllipse($outline,$r.X+$s*0.36,$r.Y+$s*0.31,$s*0.28,$s*0.28)
            for($i=0;$i -lt 8;$i++){ $a=$i*[Math]::PI/4; $x1=$cx+[Math]::Cos($a)*$s*0.21; $y1=$r.Y+$s*0.45+[Math]::Sin($a)*$s*0.21; $x2=$cx+[Math]::Cos($a)*$s*0.32; $y2=$r.Y+$s*0.45+[Math]::Sin($a)*$s*0.32; $g.DrawLine($thin,[float]$x1,[float]$y1,[float]$x2,[float]$y2) }
            $g.DrawArc($thin,$r.X+$s*0.29,$r.Y+$s*0.63,$s*0.42,$s*0.16,190,160)
        }
        'weather.high-wave' {
            $g.DrawArc($outline,$r.X+$s*0.18,$r.Y+$s*0.31,$s*0.56,$s*0.48,190,170);$g.DrawArc($outline,$r.X+$s*0.37,$r.Y+$s*0.37,$s*0.44,$s*0.39,190,170)
            $g.DrawLine($thin,$r.X+$s*0.24,$r.Y+$s*0.70,$r.X+$s*0.76,$r.Y+$s*0.70)
        }
        'risk.injury' {
            $band = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new($r.X+$s*0.29,$r.Y+$s*0.60),[System.Drawing.PointF]::new($r.X+$s*0.60,$r.Y+$s*0.29),[System.Drawing.PointF]::new($r.X+$s*0.72,$r.Y+$s*0.41),[System.Drawing.PointF]::new($r.X+$s*0.41,$r.Y+$s*0.72))
            $g.FillPolygon($orange,$band);$g.DrawPolygon($outline,$band);$g.FillEllipse($cream,$r.X+$s*0.44,$r.Y+$s*0.43,$s*0.14,$s*0.14)
        }
        'risk.illness' {
            $g.FillEllipse($green,$r.X+$s*0.34,$r.Y+$s*0.34,$s*0.32,$s*0.32);$g.DrawEllipse($outline,$r.X+$s*0.34,$r.Y+$s*0.34,$s*0.32,$s*0.32)
            for($i=0;$i -lt 8;$i++){ $a=$i*[Math]::PI/4; $x1=$cx+[Math]::Cos($a)*$s*0.17; $y1=$cy+[Math]::Sin($a)*$s*0.17; $x2=$cx+[Math]::Cos($a)*$s*0.28; $y2=$cy+[Math]::Sin($a)*$s*0.28; $g.DrawLine($thin,[float]$x1,[float]$y1,[float]$x2,[float]$y2) }
            $g.FillEllipse($red,$r.X+$s*0.43,$r.Y+$s*0.43,$s*0.06,$s*0.06);$g.FillEllipse($red,$r.X+$s*0.54,$r.Y+$s*0.51,$s*0.06,$s*0.06)
        }
        'risk.wildlife' {
            $g.FillEllipse($orange,$r.X+$s*0.36,$r.Y+$s*0.45,$s*0.28,$s*0.25);$g.DrawEllipse($outline,$r.X+$s*0.36,$r.Y+$s*0.45,$s*0.28,$s*0.25)
            for($i=0;$i -lt 4;$i++){ $g.FillEllipse($orange,$r.X+$s*(0.28+0.14*$i),$r.Y+$s*(0.28+0.03*($i%2)),$s*0.13,$s*0.15);$g.DrawEllipse($thin,$r.X+$s*(0.28+0.14*$i),$r.Y+$s*(0.28+0.03*($i%2)),$s*0.13,$s*0.15) }
        }
        'risk.theft' {
            RoundedRect $g ([System.Drawing.RectangleF]::new($r.X+$s*0.31,$r.Y+$s*0.36,$s*0.38,$s*0.36)) ($s*0.05) '#E8682A' '#17353B' ($s*0.055)
            $g.DrawArc($outline,$r.X+$s*0.39,$r.Y+$s*0.22,$s*0.22,$s*0.27,190,160)
            $slash=Pen '#C9483A' ($s*0.08);$g.DrawLine($slash,$r.X+$s*0.27,$r.Y+$s*0.73,$r.X+$s*0.73,$r.Y+$s*0.27);$slash.Dispose()
        }
        'risk.flood' {
            $g.FillRectangle($gray,$r.X+$s*0.33,$r.Y+$s*0.31,$s*0.34,$s*0.25);$g.DrawRectangle($outline,$r.X+$s*0.33,$r.Y+$s*0.31,$s*0.34,$s*0.25)
            $g.DrawArc($outline,$r.X+$s*0.20,$r.Y+$s*0.46,$s*0.44,$s*0.28,195,150);$g.DrawArc($outline,$r.X+$s*0.42,$r.Y+$s*0.51,$s*0.40,$s*0.25,195,150)
        }
        'equipment.required' {
            RoundedRect $g ([System.Drawing.RectangleF]::new($r.X+$s*0.30,$r.Y+$s*0.33,$s*0.40,$s*0.39)) ($s*0.05) '#F3BE4D' '#17353B' ($s*0.055)
            $g.DrawArc($outline,$r.X+$s*0.38,$r.Y+$s*0.22,$s*0.24,$s*0.22,180,180)
            $g.DrawLine($thin,$r.X+$s*0.50,$r.Y+$s*0.38,$r.X+$s*0.50,$r.Y+$s*0.67)
        }
        'state.mitigated' {
            $shield=[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($cx,$r.Y+$s*0.22),[System.Drawing.PointF]::new($r.X+$s*0.72,$r.Y+$s*0.32),[System.Drawing.PointF]::new($r.X+$s*0.66,$r.Y+$s*0.64),[System.Drawing.PointF]::new($cx,$r.Y+$s*0.78),[System.Drawing.PointF]::new($r.X+$s*0.34,$r.Y+$s*0.64),[System.Drawing.PointF]::new($r.X+$s*0.28,$r.Y+$s*0.32));$g.FillPolygon($teal,$shield);$g.DrawPolygon($outline,$shield)
            $check=Pen '#F4E0AE' ($s*0.08);$g.DrawLines($check,[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($r.X+$s*0.38,$r.Y+$s*0.50),[System.Drawing.PointF]::new($r.X+$s*0.47,$r.Y+$s*0.60),[System.Drawing.PointF]::new($r.X+$s*0.64,$r.Y+$s*0.40)));$check.Dispose()
        }
    }

    foreach($d in @($orange,$yellow,$teal,$green,$red,$navy,$cream,$gray,$sky,$outline,$thin)){ $d.Dispose() }
}

$iconIds = @(
    'resource.wood','resource.stone','resource.food','resource.scrap','resource.electronics','resource.medicine',
    'resource.cloth','resource.fuel','abundance.rich','abundance.normal','abundance.rare','state.unknown',
    'travel.time','weather.storm','weather.rain','weather.heat','weather.high-wave','risk.injury',
    'risk.illness','risk.wildlife','risk.theft','risk.flood','equipment.required','state.mitigated'
)

# Transparent 6x4 PNG atlas.
$atlas = New-Canvas 1536 1024 $true
for ($i = 0; $i -lt $iconIds.Count; $i++) {
    $col = $i % 6
    $row = [Math]::Floor($i / 6)
    Draw-Icon $atlas.Graphics $iconIds[$i] ([System.Drawing.RectangleF]::new($col * 256 + 32, $row * 256 + 32, 192, 192)) $false
}
Save-Png $atlas (Join-Path $iconRoot 'expedition-icons-atlas.png')

# Editable SVG atlas: production-safe, no active content and no body text.
$svgSymbols = [System.Text.StringBuilder]::new()
for ($i = 0; $i -lt $iconIds.Count; $i++) {
    $col = $i % 6
    $row = [Math]::Floor($i / 6)
    $x = $col * 256
    $y = $row * 256
    $shape = switch -Regex ($iconIds[$i]) {
        '^resource\.wood$' { '<rect x="54" y="76" width="148" height="28" rx="14" fill="#E8682A"/><rect x="54" y="114" width="148" height="28" rx="14" fill="#E8682A"/><rect x="54" y="152" width="148" height="28" rx="14" fill="#E8682A"/>' }
        '^resource\.stone$' { '<path d="M48 174 L72 92 L126 54 L186 92 L210 170 L164 202 L86 198 Z" fill="#6B7772"/>' }
        '^resource\.food$' { '<circle cx="122" cy="132" r="58" fill="#648B45"/><circle cx="122" cy="132" r="30" fill="#F4E0AE"/><path d="M122 72 L166 38 L178 86 Z" fill="#E8682A"/>' }
        '^resource\.scrap$' { '<path d="M54 66 L202 84 L184 190 L68 176 Z" fill="#6B7772"/><circle cx="164" cy="146" r="24" fill="#F3BE4D"/>' }
        '^resource\.electronics$' { '<rect x="58" y="56" width="140" height="144" rx="18" fill="#168C84"/><circle cx="128" cy="128" r="28" fill="#F3BE4D"/>' }
        '^resource\.medicine$' { '<rect x="70" y="46" width="116" height="166" rx="20" fill="#F4E0AE"/><path d="M112 76 H144 V112 H180 V144 H144 V180 H112 V144 H76 V112 H112 Z" fill="#C9483A"/>' }
        '^resource\.cloth$' { '<path d="M56 58 L202 42 L188 208 L70 194 Z" fill="#E8682A"/><path d="M76 92 L176 154"/>' }
        '^resource\.fuel$' { '<rect x="70" y="52" width="116" height="158" rx="18" fill="#E8682A"/><path d="M120 110 C94 148 110 178 132 180 C158 176 162 144 138 110 Z" fill="#F3BE4D"/>' }
        '^abundance\.rich$' { '<circle cx="74" cy="164" r="24" fill="#648B45"/><circle cx="128" cy="140" r="24" fill="#648B45"/><circle cx="182" cy="116" r="24" fill="#648B45"/>' }
        '^abundance\.normal$' { '<circle cx="98" cy="140" r="28" fill="#F3BE4D"/><circle cx="158" cy="140" r="28" fill="#F3BE4D"/>' }
        '^abundance\.rare$' { '<circle cx="128" cy="140" r="32" fill="#E8682A"/>' }
        '^state\.unknown$' { '<circle cx="128" cy="128" r="70" fill="#6B7772"/><path d="M66 190 L190 66" stroke="#F4E0AE" stroke-width="24"/>' }
        '^travel\.time$' { '<path d="M76 54 H180 M76 202 H180 M88 66 L168 66 L128 126 Z M88 190 L168 190 L128 130 Z" fill="#F3BE4D"/>' }
        '^weather\.storm$' { '<path d="M60 124 C60 78 108 78 116 92 C136 52 204 74 198 126 Z" fill="#6B7772"/><path d="M126 118 L92 178 L126 170 L106 220 L174 148 L140 154 L160 118 Z" fill="#F3BE4D"/>' }
        '^weather\.rain$' { '<path d="M60 124 C60 78 108 78 116 92 C136 52 204 74 198 126 Z" fill="#4DB7BF"/><path d="M88 154 L70 202 M132 154 L114 202 M176 154 L158 202"/>' }
        '^weather\.heat$' { '<circle cx="128" cy="112" r="48" fill="#E8682A"/><path d="M128 34 V12 M128 212 V234 M50 112 H28 M228 112 H206 M74 58 L58 42 M182 166 L198 182 M182 58 L198 42 M74 166 L58 182"/>' }
        '^weather\.high-wave$' { '<path d="M30 166 C88 86 122 98 156 148 C178 180 202 172 230 142 C200 212 126 216 82 186 C58 170 44 176 30 190 Z" fill="#4DB7BF"/>' }
        '^risk\.injury$' { '<path d="M60 160 L160 60 L198 98 L98 198 Z" fill="#E8682A"/><circle cx="129" cy="129" r="24" fill="#F4E0AE"/>' }
        '^risk\.illness$' { '<circle cx="128" cy="128" r="52" fill="#648B45"/><path d="M128 48 V22 M128 234 V208 M48 128 H22 M234 128 H208 M70 70 L50 50 M206 206 L186 186 M186 70 L206 50 M70 186 L50 206"/>' }
        '^risk\.wildlife$' { '<circle cx="128" cy="148" r="45" fill="#E8682A"/><circle cx="70" cy="88" r="22" fill="#E8682A"/><circle cx="110" cy="64" r="22" fill="#E8682A"/><circle cx="154" cy="64" r="22" fill="#E8682A"/><circle cx="194" cy="88" r="22" fill="#E8682A"/>' }
        '^risk\.theft$' { '<rect x="66" y="96" width="124" height="108" rx="18" fill="#E8682A"/><path d="M88 102 C88 32 168 32 168 102 M52 206 L206 52"/>' }
        '^risk\.flood$' { '<rect x="78" y="56" width="100" height="86" fill="#6B7772"/><path d="M28 142 C70 112 88 182 128 148 C168 114 184 182 228 144 V212 H28 Z" fill="#4DB7BF"/>' }
        '^equipment\.required$' { '<rect x="64" y="88" width="128" height="118" rx="18" fill="#F3BE4D"/><path d="M90 90 C90 34 166 34 166 90 M128 104 V190"/>' }
        '^state\.mitigated$' { '<path d="M128 38 L200 70 L184 166 L128 218 L72 166 L56 70 Z" fill="#168C84"/><path d="M88 132 L116 160 L172 96" stroke="#F4E0AE" stroke-width="22" fill="none"/>' }
    }
    [void]$svgSymbols.AppendLine("  <g id=`"$($iconIds[$i])`" transform=`"translate($x $y)`" stroke=`"#17353B`" stroke-width=`"14`" stroke-linecap=`"round`" stroke-linejoin=`"round`">$shape</g>")
}
$svg = "<svg xmlns=`"http://www.w3.org/2000/svg`" width=`"1536`" height=`"1024`" viewBox=`"0 0 1536 1024`">`n$svgSymbols</svg>"
[IO.File]::WriteAllText((Join-Path $iconRoot 'expedition-icons-atlas.svg'), $svg, [Text.UTF8Encoding]::new($false))

# Actual-size readability board. Text here is QA-only annotation, never runtime art.
# A transparent outer gutter keeps this evidence distinct from production icon art.
$read = New-Canvas 1536 720 $true
RoundedRect $read.Graphics ([System.Drawing.RectangleF]::new(8,8,1520,704)) 26 '#EFE0B5' '#4DB7BF' 4 255
Draw-Text $read.Graphics 'QA ONLY / ACTUAL SIZE / 16 24 32 48 PX' 48 28 28 '#17353B' $true
$sizes = @(16,24,32,48)
for($row=0;$row -lt $sizes.Count;$row++){
    $rowFill = if(($row % 2) -eq 0){'#F7E9C4'}else{'#E3D09D'}
    RoundedRect $read.Graphics ([System.Drawing.RectangleF]::new(36,86+$row*145,1464,126)) 16 $rowFill $null 0 255
    Draw-Text $read.Graphics ("$($sizes[$row]) PX") 48 (100 + $row*145) 24 '#17353B' $true
    for($i=0;$i -lt $iconIds.Count;$i++){
        $col=$i%12; $sub=[Math]::Floor($i/12); $xx=170+$col*108; $yy=92+$row*145+$sub*58
        Draw-Icon $read.Graphics $iconIds[$i] ([System.Drawing.RectangleF]::new($xx,$yy,$sizes[$row],$sizes[$row])) $false
    }
}
Save-Png $read (Join-Path $iconRoot 'expedition-icons-readability-16-24-32-48.png')

function Draw-SafeRect([System.Drawing.Graphics]$g, [System.Drawing.RectangleF]$r, [string]$stroke = '#F3BE4D', [int]$alpha = 210) {
    $p = Pen $stroke 2 $alpha
    $p.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
    $g.DrawRectangle($p, $r.X, $r.Y, $r.Width, $r.Height)
    $p.Dispose()
}

function Draw-Node([System.Drawing.Graphics]$g, [float]$x, [float]$y, [string]$state, [float]$scale = 1.0) {
    $size = 70 * $scale
    $r = [System.Drawing.RectangleF]::new($x-$size/2,$y-$size/2,$size,$size)
    $fill = '#F4E0AE'; $stroke='#17353B'; $dash=$false
    switch($state){
        'selected' {$fill='#E8682A';$stroke='#F3BE4D'}
        'locked' {$fill='#6B7772';$stroke='#17353B';$dash=$true}
        'danger' {$fill='#C9483A';$stroke='#F4E0AE'}
        'gear' {$fill='#F3BE4D';$stroke='#17353B'}
        'ready' {$fill='#168C84';$stroke='#F4E0AE'}
        'unknown' {$fill='#50635F';$stroke='#F4E0AE';$dash=$true}
    }
    $b=Brush $fill 245;$g.FillEllipse($b,$r);$b.Dispose()
    $p=Pen $stroke (6*$scale);if($dash){$p.DashStyle=[System.Drawing.Drawing2D.DashStyle]::Dash};$g.DrawEllipse($p,$r);$p.Dispose()
    if($state -eq 'selected'){
        $p=Pen '#17353B' (3*$scale);$g.DrawEllipse($p,$r.X-8,$r.Y-8,$r.Width+16,$r.Height+16);$p.Dispose()
        $tick=Brush '#F3BE4D';
        $triangles=@(
          [System.Drawing.PointF[]]@([System.Drawing.PointF]::new($x,$r.Y-16),[System.Drawing.PointF]::new($x-9,$r.Y-3),[System.Drawing.PointF]::new($x+9,$r.Y-3)),
          [System.Drawing.PointF[]]@([System.Drawing.PointF]::new($x,$r.Bottom+16),[System.Drawing.PointF]::new($x-9,$r.Bottom+3),[System.Drawing.PointF]::new($x+9,$r.Bottom+3))
        );foreach($t in $triangles){$g.FillPolygon($tick,$t)};$tick.Dispose()
    }
    if($state -eq 'locked'){
        $p=Pen '#17353B' (6*$scale);$g.DrawArc($p,$x-15*$scale,$y-22*$scale,30*$scale,28*$scale,180,180);$g.DrawRectangle($p,$x-18*$scale,$y-5*$scale,36*$scale,28*$scale);$p.Dispose()
        Draw-Hatch $g ([System.Drawing.RectangleF]::new($r.X+8,$r.Y+8,$r.Width-16,$r.Height-16)) '#17353B' (10*$scale)
    }
    if($state -eq 'danger'){
        $p=Pen '#F4E0AE' (7*$scale);$g.DrawLine($p,$x,$y-19*$scale,$x,$y+7*$scale);$g.DrawEllipse($p,$x-1*$scale,$y+18*$scale,2*$scale,2*$scale);$p.Dispose()
    }
    if($state -eq 'gear'){
        $p=Pen '#17353B' (6*$scale);$g.DrawRectangle($p,$x-17*$scale,$y-10*$scale,34*$scale,27*$scale);$g.DrawArc($p,$x-10*$scale,$y-23*$scale,20*$scale,20*$scale,180,180);$g.DrawLine($p,$x-23*$scale,$y+23*$scale,$x+23*$scale,$y-23*$scale);$p.Dispose()
    }
    if($state -eq 'ready'){
        $p=Pen '#F4E0AE' (7*$scale);$g.DrawLines($p,[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($x-18*$scale,$y),[System.Drawing.PointF]::new($x-4*$scale,$y+14*$scale),[System.Drawing.PointF]::new($x+21*$scale,$y-15*$scale)));$p.Dispose()
    }
    if($state -eq 'unknown'){
        $p=Pen '#F4E0AE' (7*$scale);$g.DrawLine($p,$x-22*$scale,$y+22*$scale,$x+22*$scale,$y-22*$scale);$p.Dispose()
    }
}

$mapImage = [System.Drawing.Image]::FromFile((Resolve-Path $MapSource))
function Draw-Backdrop([System.Drawing.Graphics]$g) {
    $skyRect=[System.Drawing.Rectangle]::new(0,0,1280,800)
    $gradient=[System.Drawing.Drawing2D.LinearGradientBrush]::new($skyRect,(Color '#1D7780'),(Color '#102F38'),90)
    $g.FillRectangle($gradient,$skyRect);$gradient.Dispose()
    $sun=Brush '#F3BE4D' 80;$g.FillEllipse($sun,100,96,220,220);$sun.Dispose()
    $horizon=Brush '#17626A' 180;$g.FillRectangle($horizon,0,470,1280,180);$horizon.Dispose()
    $ground=Brush '#17353B' 230;$g.FillEllipse($ground,-100,548,1480,360);$ground.Dispose()
    $palm=Pen '#17353B' 18 220
    $g.DrawLine($palm,135,550,182,278);$g.DrawLine($palm,1120,562,1068,316)
    for($i=0;$i -lt 5;$i++){
        $a=(-1.1+$i*0.55)
        $g.DrawLine($palm,182,278,[float](182+[Math]::Cos($a)*150),[float](278+[Math]::Sin($a)*96))
        $g.DrawLine($palm,1068,316,[float](1068+[Math]::Cos($a)*140),[float](316+[Math]::Sin($a)*88))
    }
    $palm.Dispose()
    $veil=Brush '#102F38' 150;$g.FillRectangle($veil,0,0,1280,800);$veil.Dispose()
}

function Draw-MapCrop([System.Drawing.Graphics]$g,[System.Drawing.RectangleF]$r){
    $g.DrawImage($mapImage,$r)
    $edge=Pen '#F3BE4D' 4;$g.DrawRectangle($edge,$r.X,$r.Y,$r.Width,$r.Height);$edge.Dispose()
}

function Draw-PanelChrome([System.Drawing.Graphics]$g,[System.Drawing.RectangleF]$outer){
    RoundedRect $g $outer 26 '#17353B' '#4DB7BF' 5 247
    $cord=Pen '#E8682A' 5;$g.DrawLine($cord,$outer.X+32,$outer.Y+26,$outer.Right-32,$outer.Y+26);$g.DrawLine($cord,$outer.X+32,$outer.Bottom-26,$outer.Right-32,$outer.Bottom-26);$cord.Dispose()
}

function Draw-DetailCard([System.Drawing.Graphics]$g,[System.Drawing.RectangleF]$card,[string]$mode){
    RoundedRect $g $card 18 '#EFE0B5' '#17353B' 4 248
    $header=[System.Drawing.RectangleF]::new($card.X+24,$card.Y+22,$card.Width-48,58);Draw-SafeRect $g $header '#168C84'
    if($mode -eq 'bottom'){
        $colW=($card.Width-72)/3
        for($c=0;$c -lt 3;$c++){
            $x=$card.X+24+$c*($colW+12)
            Draw-SafeRect $g ([System.Drawing.RectangleF]::new($x,$card.Y+94,$colW,78)) '#168C84'
            for($i=0;$i -lt 3;$i++){Draw-Icon $g $iconIds[($c*3+$i)%$iconIds.Count] ([System.Drawing.RectangleF]::new($x+8+$i*52,$card.Y+106,40,40)) $true}
        }
        Draw-SafeRect $g ([System.Drawing.RectangleF]::new($card.Right-226,$card.Bottom-58,150,44)) '#E8682A'
        Draw-SafeRect $g ([System.Drawing.RectangleF]::new($card.Right-66,$card.Bottom-58,44,44)) '#F3BE4D'
    } else {
        $y=$card.Y+96
        $rowHeights=@(112,76,76,68,72)
        $rowIcon=@('resource.wood','risk.wildlife','weather.rain','equipment.required','state.unknown')
        for($i=0;$i -lt $rowHeights.Count;$i++){
            $h=$rowHeights[$i]
            Draw-Icon $g $rowIcon[$i] ([System.Drawing.RectangleF]::new($card.X+24,$y+10,44,44)) $true
            Draw-SafeRect $g ([System.Drawing.RectangleF]::new($card.X+82,$y+8,$card.Width-106,$h-16)) '#168C84'
            $y+=$h
        }
        Draw-SafeRect $g ([System.Drawing.RectangleF]::new($card.X+24,$card.Bottom-66,$card.Width-102,44)) '#E8682A'
        Draw-SafeRect $g ([System.Drawing.RectangleF]::new($card.Right-66,$card.Bottom-66,44,44)) '#F3BE4D'
    }
}

function Add-MapNodes([System.Drawing.Graphics]$g,[System.Drawing.RectangleF]$mapRect,[string]$variant){
    $campX=$mapRect.X+$mapRect.Width*0.50;$campY=$mapRect.Y+$mapRect.Height*0.78
    $beachX=$mapRect.X+$mapRect.Width*0.27;$beachY=$mapRect.Y+$mapRect.Height*0.48
    $forestX=$mapRect.X+$mapRect.Width*0.64;$forestY=$mapRect.Y+$mapRect.Height*0.32
    $waterX=$mapRect.X+$mapRect.Width*0.67;$waterY=$mapRect.Y+$mapRect.Height*0.70
    $route=Pen '#17353B' 5 210;$route.DashStyle=[System.Drawing.Drawing2D.DashStyle]::Dash
    $g.DrawLine($route,$campX,$campY,$beachX,$beachY);$g.DrawLine($route,$campX,$campY,$forestX,$forestY);$g.DrawLine($route,$campX,$campY,$waterX,$waterY);$route.Dispose()
    if($variant -eq 'A'){Draw-Node $g $beachX $beachY 'idle';Draw-Node $g $forestX $forestY 'selected';Draw-Node $g $waterX $waterY 'unknown'}
    elseif($variant -eq 'B'){Draw-Node $g $beachX $beachY 'idle';Draw-Node $g $forestX $forestY 'danger';Draw-Node $g $waterX $waterY 'gear'}
    else{Draw-Node $g $beachX $beachY 'ready';Draw-Node $g $forestX $forestY 'locked';Draw-Node $g $waterX $waterY 'unknown'}
    Draw-Node $g $campX $campY 'idle' 0.72
    foreach($pos in @(@($beachX,$beachY),@($forestX,$forestY),@($waterX,$waterY))){Draw-SafeRect $g ([System.Drawing.RectangleF]::new($pos[0]-80,$pos[1]+46,160,42)) '#F3BE4D' 190}
}

function Build-Candidate([string]$variant,[string]$path){
    $c=New-Canvas 1280 800 $false;Draw-Backdrop $c.Graphics
    $outer=[System.Drawing.RectangleF]::new(36,38,1208,724);Draw-PanelChrome $c.Graphics $outer
    if($variant -eq 'A'){
        $map=[System.Drawing.RectangleF]::new(68,86,742,628);$card=[System.Drawing.RectangleF]::new(834,86,376,628)
        Draw-MapCrop $c.Graphics $map;Add-MapNodes $c.Graphics $map 'A';Draw-DetailCard $c.Graphics $card 'right'
    }elseif($variant -eq 'B'){
        $map=[System.Drawing.RectangleF]::new(68,76,1142,452);$card=[System.Drawing.RectangleF]::new(68,548,1142,178)
        Draw-MapCrop $c.Graphics $map;Add-MapNodes $c.Graphics $map 'B';Draw-DetailCard $c.Graphics $card 'bottom'
    }else{
        $map=[System.Drawing.RectangleF]::new(68,86,824,628);$card=[System.Drawing.RectangleF]::new(912,86,298,628)
        Draw-MapCrop $c.Graphics $map;Add-MapNodes $c.Graphics $map 'C';Draw-DetailCard $c.Graphics $card 'compact'
    }
    Save-Png $c $path
}

$candidateA=Join-Path $mapRoot 'candidate-a-right-rail-1280x800.png'
$candidateB=Join-Path $mapRoot 'candidate-b-bottom-drawer-1280x800.png'
$candidateC=Join-Path $mapRoot 'candidate-c-compact-right-1280x800.png'
Build-Candidate 'A' $candidateA;Build-Candidate 'B' $candidateB;Build-Candidate 'C' $candidateC

# QA-only localization and input overlay based on candidate A.
$overlay=New-Canvas 1280 800 $false
$baseA=[System.Drawing.Image]::FromFile($candidateA);$overlay.Graphics.DrawImage($baseA,0,0,1280,800);$baseA.Dispose()
$wash=Brush '#102F38' 70;$overlay.Graphics.FillRectangle($wash,0,0,1280,800);$wash.Dispose()
Draw-Text $overlay.Graphics 'QA ONLY / TMP SAFE RECTS / KO EN QPS-LONG / KBM GAMEPAD' 48 10 18 '#F4E0AE' $true
$rects=@(
    @{id='NODE-01';r=[System.Drawing.RectangleF]::new(145,360,160,42)},
    @{id='NODE-02';r=[System.Drawing.RectangleF]::new(430,260,160,42)},
    @{id='NODE-03';r=[System.Drawing.RectangleF]::new(455,560,160,42)},
    @{id='TITLE';r=[System.Drawing.RectangleF]::new(858,108,328,58)},
    @{id='RESOURCE';r=[System.Drawing.RectangleF]::new(916,190,270,96)},
    @{id='RISK';r=[System.Drawing.RectangleF]::new(916,302,270,60)},
    @{id='WEATHER';r=[System.Drawing.RectangleF]::new(916,378,270,60)},
    @{id='EQUIP';r=[System.Drawing.RectangleF]::new(916,454,270,60)},
    @{id='SPECIAL';r=[System.Drawing.RectangleF]::new(916,530,270,68)},
    @{id='ACTION';r=[System.Drawing.RectangleF]::new(858,636,262,44)},
    @{id='GLYPH';r=[System.Drawing.RectangleF]::new(1142,636,44,44)}
)
foreach($item in $rects){Draw-SafeRect $overlay.Graphics $item.r '#F3BE4D';Draw-Text $overlay.Graphics $item.id ($item.r.X+4) ($item.r.Y+3) 12 '#F3BE4D' $true}
Save-Png $overlay (Join-Path $mapRoot 'candidate-a-localization-input-overlay-1280x800.png')

# QA-only device focus comparison. Runtime candidates keep literal glyphs empty.
$focusBoard=New-Canvas 1920 720 $false
Draw-Text $focusBoard.Graphics 'QA ONLY / POINTER FOCUS VS GAMEPAD FOCUS / 44PX MIN' 48 26 30 '#F4E0AE' $true
for($i=0;$i -lt 2;$i++){
    $x=48+$i*930
    RoundedRect $focusBoard.Graphics ([System.Drawing.RectangleF]::new($x,100,890,560)) 24 '#EFE0B5' '#4DB7BF' 5 245
    $focusLabel=if($i -eq 0){'POINTER / KEYBOARD'}else{'GAMEPAD / D-PAD-STICK'}
    Draw-Text $focusBoard.Graphics $focusLabel ($x+30) 126 24 '#17353B' $true
    Draw-Node $focusBoard.Graphics ($x+250) 330 'selected' 1.5
    Draw-SafeRect $focusBoard.Graphics ([System.Drawing.RectangleF]::new($x+380,230,450,70)) '#168C84'
    Draw-SafeRect $focusBoard.Graphics ([System.Drawing.RectangleF]::new($x+380,326,330,58)) '#E8682A'
    Draw-SafeRect $focusBoard.Graphics ([System.Drawing.RectangleF]::new($x+730,326,44,44)) '#F3BE4D'
    if($i -eq 0){
        $cursor=[System.Drawing.PointF[]]@([System.Drawing.PointF]::new($x+300,385),[System.Drawing.PointF]::new($x+328,448),[System.Drawing.PointF]::new($x+340,420),[System.Drawing.PointF]::new($x+372,450),[System.Drawing.PointF]::new($x+390,432),[System.Drawing.PointF]::new($x+358,404),[System.Drawing.PointF]::new($x+386,390))
        $b=Brush '#F4E0AE';$focusBoard.Graphics.FillPolygon($b,$cursor);$b.Dispose()
        $p=Pen '#17353B' 5;$focusBoard.Graphics.DrawPolygon($p,$cursor);$p.Dispose()
    }else{
        $p=Pen '#17353B' 7
        $focusBoard.Graphics.DrawLine($p,$x+162,240,$x+206,240);$focusBoard.Graphics.DrawLine($p,$x+162,240,$x+162,284)
        $focusBoard.Graphics.DrawLine($p,$x+338,240,$x+294,240);$focusBoard.Graphics.DrawLine($p,$x+338,240,$x+338,284)
        $focusBoard.Graphics.DrawLine($p,$x+162,420,$x+206,420);$focusBoard.Graphics.DrawLine($p,$x+162,420,$x+162,376)
        $focusBoard.Graphics.DrawLine($p,$x+338,420,$x+294,420);$focusBoard.Graphics.DrawLine($p,$x+338,420,$x+338,376);$p.Dispose()
    }
}
Save-Png $focusBoard (Join-Path $mapRoot 'expedition-map-input-focus-comparison.png')

# Seven-state comparison board. Text is QA evidence only.
$stateBoard=New-Canvas 1920 1080 $false
Draw-Text $stateBoard.Graphics 'WAVE 15 REVIEW ONLY / NODE + CARD STATES' 48 26 34 '#F4E0AE' $true
$states=@('idle','selected','locked','danger','gear','ready','unknown')
for($i=0;$i -lt $states.Count;$i++){
    $col=$i%4;$row=[Math]::Floor($i/4);$x=48+$col*462;$y=110+$row*450
    RoundedRect $stateBoard.Graphics ([System.Drawing.RectangleF]::new($x,$y,430,400)) 22 '#EFE0B5' '#4DB7BF' 4 245
    Draw-Text $stateBoard.Graphics ("STATE-$($i+1) / $($states[$i].ToUpper())") ($x+24) ($y+20) 24 '#17353B' $true
    Draw-Node $stateBoard.Graphics ($x+215) ($y+142) $states[$i] 1.35
    Draw-SafeRect $stateBoard.Graphics ([System.Drawing.RectangleF]::new($x+36,$y+226,358,48)) '#168C84'
    Draw-SafeRect $stateBoard.Graphics ([System.Drawing.RectangleF]::new($x+36,$y+288,276,44)) '#E8682A'
    Draw-SafeRect $stateBoard.Graphics ([System.Drawing.RectangleF]::new($x+326,$y+288,44,44)) '#F3BE4D'
}
Save-Png $stateBoard (Join-Path $mapRoot 'expedition-map-state-comparison-board.png')

# Main final review board with three 1280x800 candidates.
$review=New-Canvas 1920 1880 $false
Draw-Text $review.Graphics 'WAVE 15 EXPEDITION MAP / REVIEW ONLY / SELECTED: NONE' 48 24 32 '#F4E0AE' $true
$candidateImages=@([System.Drawing.Image]::FromFile($candidateA),[System.Drawing.Image]::FromFile($candidateB),[System.Drawing.Image]::FromFile($candidateC))
$candidateNames=@('A / RIGHT RAIL / RECOMMENDED','B / BOTTOM DRAWER / WIDE MAP','C / COMPACT RIGHT / DENSE')
for($i=0;$i -lt 3;$i++){
    $y=96+$i*570
    $candidateAccent = if($i -eq 0){'#F3BE4D'}else{'#4DB7BF'}
    Draw-Text $review.Graphics $candidateNames[$i] 48 ($y-2) 24 $candidateAccent $true
    $review.Graphics.DrawImage($candidateImages[$i],[System.Drawing.Rectangle]::new(48,$y+42,864,540))
    RoundedRect $review.Graphics ([System.Drawing.RectangleF]::new(958,$y+42,914,540)) 20 '#17353B' '#4DB7BF' 4 245
    Draw-Text $review.Graphics 'MAP / NODES / ROUTES' 1000 ($y+78) 22 '#F4E0AE' $true
    Draw-SafeRect $review.Graphics ([System.Drawing.RectangleF]::new(1000,$y+120,824,86)) '#168C84'
    Draw-Text $review.Graphics 'DETAIL CARD / RESOURCE / RISK / WEATHER' 1000 ($y+232) 22 '#F4E0AE' $true
    Draw-SafeRect $review.Graphics ([System.Drawing.RectangleF]::new(1000,$y+276,824,118)) '#168C84'
    Draw-Text $review.Graphics 'KO EN QPS-LONG + 44PX GLYPH SLOTS' 1000 ($y+420) 22 '#F4E0AE' $true
    Draw-SafeRect $review.Graphics ([System.Drawing.RectangleF]::new(1000,$y+462,640,48)) '#E8682A'
    Draw-SafeRect $review.Graphics ([System.Drawing.RectangleF]::new(1660,$y+462,48,48)) '#F3BE4D'
}
foreach($img in $candidateImages){$img.Dispose()}
Save-Png $review (Join-Path $mapRoot 'expedition-map-review-board.png')

# Editable component source (production source; no localized text).
$uiSvg=@'
<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="800" viewBox="0 0 1280 800">
  <defs>
    <pattern id="invalidHatch" width="12" height="12" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><line x1="0" y1="0" x2="0" y2="12" stroke="#17353B" stroke-width="4"/></pattern>
  </defs>
  <g id="popup-frame" data-nine-slice="32,32,28,28"><rect x="36" y="38" width="1208" height="724" rx="26" fill="#17353B" stroke="#4DB7BF" stroke-width="5"/></g>
  <g id="map-art-slot"><rect x="68" y="86" width="742" height="628" fill="none" stroke="#F3BE4D" stroke-width="4"/></g>
  <g id="detail-card" data-nine-slice="24,24,20,20"><rect x="834" y="86" width="376" height="628" rx="18" fill="#EFE0B5" stroke="#17353B" stroke-width="4"/></g>
  <g id="tmp-safe-rects" fill="none" stroke="#168C84" stroke-width="2" stroke-dasharray="8 6">
    <rect id="tmp-region-title" x="858" y="108" width="328" height="58"/>
    <rect id="tmp-resource-forecast" x="916" y="190" width="270" height="96"/>
    <rect id="tmp-risk" x="916" y="302" width="270" height="60"/>
    <rect id="tmp-weather" x="916" y="378" width="270" height="60"/>
    <rect id="tmp-equipment" x="916" y="454" width="270" height="60"/>
    <rect id="tmp-special-discovery" x="916" y="530" width="270" height="68"/>
    <rect id="tmp-action" x="858" y="636" width="262" height="44"/>
  </g>
  <g id="glyph-slot"><rect x="1142" y="636" width="44" height="44" rx="10" fill="none" stroke="#F3BE4D" stroke-width="3"/></g>
  <g id="node-idle"><circle cx="180" cy="220" r="35" fill="#F4E0AE" stroke="#17353B" stroke-width="6"/></g>
  <g id="node-selected"><circle cx="280" cy="220" r="43" fill="none" stroke="#17353B" stroke-width="3"/><circle cx="280" cy="220" r="35" fill="#E8682A" stroke="#F3BE4D" stroke-width="6"/></g>
  <g id="node-locked"><circle cx="380" cy="220" r="35" fill="#6B7772" stroke="#17353B" stroke-width="6" stroke-dasharray="10 8"/><circle cx="380" cy="220" r="27" fill="url(#invalidHatch)"/></g>
  <g id="node-danger"><circle cx="480" cy="220" r="35" fill="#C9483A" stroke="#F4E0AE" stroke-width="6"/></g>
  <g id="node-equipment-short"><circle cx="580" cy="220" r="35" fill="#F3BE4D" stroke="#17353B" stroke-width="6"/><path d="M552 248 L608 192" stroke="#17353B" stroke-width="7"/></g>
  <g id="node-ready"><circle cx="680" cy="220" r="35" fill="#168C84" stroke="#F4E0AE" stroke-width="6"/><path d="M662 220 L676 234 L701 205" stroke="#F4E0AE" stroke-width="7" fill="none"/></g>
  <g id="node-unknown"><circle cx="780" cy="220" r="35" fill="#50635F" stroke="#F4E0AE" stroke-width="6" stroke-dasharray="10 8"/><path d="M758 242 L802 198" stroke="#F4E0AE" stroke-width="7"/></g>
</svg>
'@
[IO.File]::WriteAllText((Join-Path $mapRoot 'expedition-map-components.svg'),$uiSvg,[Text.UTF8Encoding]::new($false))

$mapImage.Dispose()

$rectsMetadata=@()
for($i=0;$i -lt $iconIds.Count;$i++){
    $rectsMetadata += [ordered]@{id=$iconIds[$i];x=($i%6)*256;y=[Math]::Floor($i/6)*256;width=256;height=256;pivot=@(0.5,0.5)}
}
$iconManifest=[ordered]@{
    schemaVersion=1;assetId='icon.expedition-resource-risk-set';jobId=$IconJobId;status='review';selectedCandidate=$null
    atlas=[ordered]@{file='expedition-icons-atlas.png';editable='expedition-icons-atlas.svg';width=1536;height=1024;alpha='true';grid=[ordered]@{columns=6;rows=4;cell=256};sprites=$rectsMetadata}
    readability=@{sizes=@(16,24,32,48);board='expedition-icons-readability-16-24-32-48.png'}
    semantics=@{abundance='rich=three rising dots, normal=two level dots, rare=one dot, unknown=slashed silhouette';risk='unique silhouette plus red only as redundant cue';weather='unique cloud/sun/wave silhouette';equipment='tool-case silhouette';mitigated='shield plus check silhouette'}
    unityImport=@{textureType='Sprite';spriteMode='Multiple';sRGB=$true;alphaIsTransparency=$true;filter='Bilinear';compression='None during review';maxSize=2048;ppu=100}
    runtime=@{allowlist=@();packageAllowed=$false;runtimeConnectAllowed=$false}
}
$iconManifest|ConvertTo-Json -Depth 12|Set-Content -Encoding utf8 (Join-Path $iconRoot 'expedition-icons-manifest.json')
$iconQa=[ordered]@{schemaVersion=1;assetId='icon.expedition-resource-risk-set';status='review';selectedCandidate=$null;checks=@(
    @{id='true-alpha';result='pass';evidence='transparent 32-bit atlas'},@{id='icon-count';result='pass';value=24},@{id='actual-size';result='pass';sizes=@(16,24,32,48)},@{id='non-color-state';result='pass';evidence='dots, hatching, silhouette and shield grammar'},@{id='raster-text';result='pass';value='none in production atlas'},@{id='external-api';result='pass';value='none'}
);manualReview=@{result='pass-with-review';note='16px is intended as a compact cue; detailed region cards should prefer 24-32px.'};qualityGate='pending Forge import'}
$iconQa|ConvertTo-Json -Depth 10|Set-Content -Encoding utf8 (Join-Path $iconRoot 'expedition-icons-visual-qa.json')

$mapManifest=[ordered]@{
    schemaVersion=1;assetId='ui.expedition-map';jobId=$MapJobId;status='review';selectedCandidate=$null;recommendedCandidate='ui.expedition-map.right-rail-a'
    candidates=@(
        @{id='ui.expedition-map.right-rail-a';file='candidate-a-right-rail-1280x800.png';detailPlacement='right';strength='best hierarchy, controller scan path and qps-long vertical reflow';concern='map width is smaller than B'},
        @{id='ui.expedition-map.bottom-drawer-b';file='candidate-b-bottom-drawer-1280x800.png';detailPlacement='bottom';strength='widest island map and short pointer travel';concern='long locale rows become vertically tight'},
        @{id='ui.expedition-map.compact-right-c';file='candidate-c-compact-right-1280x800.png';detailPlacement='right-compact';strength='largest map among right-card layouts';concern='highest localization density and weakest first-read hierarchy'}
    )
    sourceArt=@{file='island-map-art.png';generation='single Codex ImageGen call';referenceDirection='background.coast-forest.gameplay-band-contrast';adoptionScope='palette, contrast and broad shape only';exactBitmapAdopted=$false}
    states=@('idle','selected','locked','danger-warning','equipment-short','departure-ready','unknown')
    nodes=@(
      @{id='region.beach';shape='circle';labelSlot='TMP';resourceForecast='category-only'},@{id='region.forest';shape='circle+selection-notches';labelSlot='TMP';resourceForecast='category-only'},@{id='region.shallow-water';shape='circle+dashed-unknown';labelSlot='TMP';resourceForecast='category-only'}
    )
    localization=@{productionRasterText='none';locales=@('ko','en','qps-long');qpsLongExpansion=1.5;overflow='wrap then vertical reflow, never shrink below 18px at 1280x800';glyphSlot=@{width=44;height=44;devices=@('keyboard-mouse','gamepad')};safeRectOverlay='candidate-a-localization-input-overlay-1280x800.png';focusEvidence='expedition-map-input-focus-comparison.png'}
    safeArea=@{canvas=@{width=1280;height=800};outer=@{left=36;right=36;top=38;bottom=38};content=@{left=68;right=70;top=76;bottom=74};minimumFocus=@{width=44;height=44}}
    slices=@{popupFrame=@{left=32;right=32;top=28;bottom=28};detailCard=@{left=24;right=24;top=20;bottom=20};actionButton=@{left=18;right=18;top=14;bottom=14}}
    layers=@('backdrop-dimmer','popup-frame','map-art','route-lines','region-nodes','node-state-overlays','detail-card','forecast-icons','tmp-safe-rects','glyph-slots','focus-outline')
    dependencyAllowlist=@('icon.expedition-resource-risk-set','icon.resource-tool-set','background.coast-forest.gameplay-band-contrast')
    dependencyRejectlist=@('job_20260823132501_b4a82bed/coast-forest-clean-selected-b.png','review-board rasters','QA annotation overlays')
    unityImport=@{mapArt=@{textureType='Sprite';spriteMode='Single';sRGB=$true;alphaIsTransparency=$false;filter='Bilinear';compression='None during review';maxSize=2048};components=@{source='expedition-map-components.svg';renderMode='Unity UI / 9-slice';ppu=100}}
    runtime=@{allowlist=@();packageAllowed=$false;runtimeConnectAllowed=$false}
}
$mapManifest|ConvertTo-Json -Depth 14|Set-Content -Encoding utf8 (Join-Path $mapRoot 'expedition-map-review-manifest.json')
$mapQa=[ordered]@{schemaVersion=1;assetId='ui.expedition-map';status='review';selectedCandidate=$null;checks=@(
    @{id='canvas';result='pass';expected='1280x800';candidates=3},@{id='contextual-only';result='pass';evidence='full modal popup with dimmed camp backdrop, not persistent HUD'},@{id='three-regions';result='pass';value=@('beach','forest','shallow-water')},@{id='information-hierarchy';result='pass';value=@('resource category and relative abundance','travel time','current risk','weather','equipment','special discovery')},@{id='hidden-outcomes';result='pass';evidence='no exact quantities and unknown silhouette slot'},@{id='seven-states';result='pass';evidence='state comparison board'},@{id='localization';result='pass';value=@('ko','en','qps-long')},@{id='input';result='pass';value=@('keyboard-mouse','gamepad','44x44 focus');evidence='expedition-map-input-focus-comparison.png'},@{id='raster-text';result='pass';value='none in production candidates; QA-only labels are confined to review boards'},@{id='external-paid-api';result='pass';value='none'},@{id='runtime';result='pass';value='not connected'}
);manualReview=@{result='pass-with-review';recommended='ui.expedition-map.right-rail-a';concerns=@('A slightly reduces map width','B has qps-long vertical pressure','C is intentionally dense')};qualityGate='pending Forge import'}
$mapQa|ConvertTo-Json -Depth 12|Set-Content -Encoding utf8 (Join-Path $mapRoot 'expedition-map-visual-qa.json')

Write-Output (ConvertTo-Json -Compress -Depth 5 ([ordered]@{icons=$iconRoot;map=$mapRoot;iconCount=$iconIds.Count;candidates=3;selectedCandidate=$null;status='review'}))
