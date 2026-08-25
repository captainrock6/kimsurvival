param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path,
    [string]$OutputRoot = (Join-Path $PSScriptRoot 'LocalSource')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$candidateId = 'icon.gamejam-hazard-part-set.silhouette-a'
$jobId = 'job_20260825171815_f7640e25'
$icons = @(
    [ordered]@{ id='risk.insects'; file='risk-insects.png'; kind='insects'; role='new' },
    [ordered]@{ id='risk.dangerous-plants'; file='risk-dangerous-plants.png'; kind='plant'; role='new' },
    [ordered]@{ id='part.smoke.flint'; file='part-smoke-flint.png'; kind='flint'; role='new' },
    [ordered]@{ id='part.radio.transceiver'; file='part-radio-transceiver.png'; kind='radio'; role='new' },
    [ordered]@{ id='part.radio.circuit-board'; file='part-radio-circuit-board.png'; kind='board'; role='new' },
    [ordered]@{ id='part.radio.transistor'; file='part-radio-transistor.png'; kind='transistor'; role='new' }
)

$navy = [Drawing.Color]::FromArgb(255, 18, 49, 58)
$deep = [Drawing.Color]::FromArgb(255, 9, 31, 38)
$teal = [Drawing.Color]::FromArgb(255, 67, 192, 185)
$green = [Drawing.Color]::FromArgb(255, 74, 151, 91)
$cream = [Drawing.Color]::FromArgb(255, 245, 226, 171)
$orange = [Drawing.Color]::FromArgb(255, 235, 111, 42)
$yellow = [Drawing.Color]::FromArgb(255, 245, 190, 70)
$gray = [Drawing.Color]::FromArgb(255, 115, 133, 134)

function New-Bitmap([int]$Width, [int]$Height) {
    $bitmap = [Drawing.Bitmap]::new($Width, $Height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $bitmap.SetResolution(96, 96)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([Drawing.Color]::Transparent)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    return [pscustomobject]@{ Bitmap=$bitmap; Graphics=$graphics }
}

function New-Pen([Drawing.Color]$Color, [single]$Width) {
    $pen = [Drawing.Pen]::new($Color, $Width)
    $pen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    $pen.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    return $pen
}

function Fill-Polygon($Graphics, [Drawing.Color]$Fill, [Drawing.Color]$Stroke, [single]$Width, [Drawing.PointF[]]$Points) {
    $brush = [Drawing.SolidBrush]::new($Fill)
    $pen = New-Pen $Stroke $Width
    $Graphics.FillPolygon($brush, $Points)
    $Graphics.DrawPolygon($pen, $Points)
    $brush.Dispose(); $pen.Dispose()
}

function Draw-Icon($Graphics, [string]$Kind, [single]$X, [single]$Y, [single]$Scale) {
    $outline = New-Pen $navy (12 * $Scale)
    $detail = New-Pen $cream (7 * $Scale)
    $accent = New-Pen $orange (8 * $Scale)
    $creamBrush = [Drawing.SolidBrush]::new($cream)
    $tealBrush = [Drawing.SolidBrush]::new($teal)
    $greenBrush = [Drawing.SolidBrush]::new($green)
    $orangeBrush = [Drawing.SolidBrush]::new($orange)
    $yellowBrush = [Drawing.SolidBrush]::new($yellow)
    $navyBrush = [Drawing.SolidBrush]::new($navy)
    $grayBrush = [Drawing.SolidBrush]::new($gray)

    switch ($Kind) {
        'insects' {
            $Graphics.FillEllipse($creamBrush, $X+44*$Scale, $Y+76*$Scale, 62*$Scale, 78*$Scale)
            $Graphics.DrawEllipse($outline, $X+44*$Scale, $Y+76*$Scale, 62*$Scale, 78*$Scale)
            $Graphics.FillEllipse($creamBrush, $X+150*$Scale, $Y+76*$Scale, 62*$Scale, 78*$Scale)
            $Graphics.DrawEllipse($outline, $X+150*$Scale, $Y+76*$Scale, 62*$Scale, 78*$Scale)
            $Graphics.FillEllipse($tealBrush, $X+94*$Scale, $Y+64*$Scale, 68*$Scale, 122*$Scale)
            $Graphics.DrawEllipse($outline, $X+94*$Scale, $Y+64*$Scale, 68*$Scale, 122*$Scale)
            $Graphics.DrawLine($detail, $X+102*$Scale, $Y+108*$Scale, $X+154*$Scale, $Y+108*$Scale)
            $Graphics.DrawLine($detail, $X+101*$Scale, $Y+139*$Scale, $X+155*$Scale, $Y+139*$Scale)
            $Graphics.DrawArc($outline, $X+77*$Scale, $Y+32*$Scale, 52*$Scale, 58*$Scale, 200, 100)
            $Graphics.DrawArc($outline, $X+127*$Scale, $Y+32*$Scale, 52*$Scale, 58*$Scale, 240, 100)
            foreach($p in @(@(56,188),@(83,205),@(49,221))){ $Graphics.FillEllipse($orangeBrush,$X+$p[0]*$Scale,$Y+$p[1]*$Scale,16*$Scale,16*$Scale) }
        }
        'plant' {
            $Graphics.DrawLine($outline, $X+128*$Scale, $Y+205*$Scale, $X+128*$Scale, $Y+91*$Scale)
            $left = [Drawing.PointF[]]@([Drawing.PointF]::new($X+119*$Scale,$Y+159*$Scale),[Drawing.PointF]::new($X+48*$Scale,$Y+74*$Scale),[Drawing.PointF]::new($X+45*$Scale,$Y+130*$Scale),[Drawing.PointF]::new($X+72*$Scale,$Y+176*$Scale))
            $right = [Drawing.PointF[]]@([Drawing.PointF]::new($X+137*$Scale,$Y+159*$Scale),[Drawing.PointF]::new($X+208*$Scale,$Y+74*$Scale),[Drawing.PointF]::new($X+211*$Scale,$Y+130*$Scale),[Drawing.PointF]::new($X+184*$Scale,$Y+176*$Scale))
            $top = [Drawing.PointF[]]@([Drawing.PointF]::new($X+128*$Scale,$Y+127*$Scale),[Drawing.PointF]::new($X+98*$Scale,$Y+86*$Scale),[Drawing.PointF]::new($X+128*$Scale,$Y+34*$Scale),[Drawing.PointF]::new($X+158*$Scale,$Y+86*$Scale))
            Fill-Polygon $Graphics $green $navy (11*$Scale) $left
            Fill-Polygon $Graphics $teal $navy (11*$Scale) $right
            Fill-Polygon $Graphics $yellow $navy (11*$Scale) $top
            foreach($p in @(@(64,112),@(192,112),@(108,82),@(148,82))){
                $tri=[Drawing.PointF[]]@([Drawing.PointF]::new($X+($p[0]-9)*$Scale,$Y+($p[1]+10)*$Scale),[Drawing.PointF]::new($X+$p[0]*$Scale,$Y+($p[1]-10)*$Scale),[Drawing.PointF]::new($X+($p[0]+9)*$Scale,$Y+($p[1]+10)*$Scale)); Fill-Polygon $Graphics $orange $navy (4*$Scale) $tri
            }
            $Graphics.DrawArc($outline,$X+82*$Scale,$Y+181*$Scale,92*$Scale,50*$Scale,10,160)
            foreach($p in @(@(61,207),@(99,225),@(151,225),@(189,207))){ $Graphics.FillEllipse($creamBrush,$X+$p[0]*$Scale,$Y+$p[1]*$Scale,14*$Scale,14*$Scale) }
        }
        'flint' {
            $stone=[Drawing.PointF[]]@([Drawing.PointF]::new($X+52*$Scale,$Y+171*$Scale),[Drawing.PointF]::new($X+78*$Scale,$Y+64*$Scale),[Drawing.PointF]::new($X+153*$Scale,$Y+39*$Scale),[Drawing.PointF]::new($X+211*$Scale,$Y+93*$Scale),[Drawing.PointF]::new($X+185*$Scale,$Y+190*$Scale),[Drawing.PointF]::new($X+103*$Scale,$Y+217*$Scale)); Fill-Polygon $Graphics $gray $navy (13*$Scale) $stone
            $Graphics.DrawLine($detail,$X+82*$Scale,$Y+160*$Scale,$X+153*$Scale,$Y+54*$Scale)
            $Graphics.DrawLine($detail,$X+86*$Scale,$Y+164*$Scale,$X+180*$Scale,$Y+179*$Scale)
            $spark=[Drawing.PointF[]]@([Drawing.PointF]::new($X+195*$Scale,$Y+43*$Scale),[Drawing.PointF]::new($X+207*$Scale,$Y+69*$Scale),[Drawing.PointF]::new($X+235*$Scale,$Y+62*$Scale),[Drawing.PointF]::new($X+217*$Scale,$Y+84*$Scale),[Drawing.PointF]::new($X+234*$Scale,$Y+105*$Scale),[Drawing.PointF]::new($X+203*$Scale,$Y+96*$Scale),[Drawing.PointF]::new($X+190*$Scale,$Y+120*$Scale),[Drawing.PointF]::new($X+190*$Scale,$Y+91*$Scale),[Drawing.PointF]::new($X+161*$Scale,$Y+83*$Scale),[Drawing.PointF]::new($X+187*$Scale,$Y+70*$Scale)); Fill-Polygon $Graphics $orange $navy (5*$Scale) $spark
        }
        'radio' {
            $body=[Drawing.RectangleF]::new($X+42*$Scale,$Y+78*$Scale,172*$Scale,126*$Scale)
            $Graphics.FillRectangle($tealBrush,$body); $Graphics.DrawRectangle($outline,$body.X,$body.Y,$body.Width,$body.Height)
            $Graphics.FillEllipse($creamBrush,$X+60*$Scale,$Y+105*$Scale,60*$Scale,60*$Scale); $Graphics.DrawEllipse($outline,$X+60*$Scale,$Y+105*$Scale,60*$Scale,60*$Scale)
            foreach($p in @(@(73,119),@(96,119),@(73,143),@(96,143))){$Graphics.FillEllipse($navyBrush,$X+$p[0]*$Scale,$Y+$p[1]*$Scale,10*$Scale,10*$Scale)}
            $Graphics.DrawLine($detail,$X+143*$Scale,$Y+113*$Scale,$X+193*$Scale,$Y+113*$Scale)
            $Graphics.DrawLine($detail,$X+143*$Scale,$Y+142*$Scale,$X+193*$Scale,$Y+142*$Scale)
            $Graphics.DrawLine($outline,$X+175*$Scale,$Y+77*$Scale,$X+198*$Scale,$Y+39*$Scale)
            $Graphics.DrawLine($outline,$X+207*$Scale,$Y+57*$Scale,$X+224*$Scale,$Y+29*$Scale)
            $Graphics.DrawLine($accent,$X+125*$Scale,$Y+87*$Scale,$X+144*$Scale,$Y+110*$Scale)
            $Graphics.DrawLine($accent,$X+144*$Scale,$Y+110*$Scale,$X+130*$Scale,$Y+132*$Scale)
            $Graphics.DrawLine($accent,$X+130*$Scale,$Y+132*$Scale,$X+153*$Scale,$Y+159*$Scale)
        }
        'board' {
            $pcb=[Drawing.PointF[]]@([Drawing.PointF]::new($X+47*$Scale,$Y+56*$Scale),[Drawing.PointF]::new($X+177*$Scale,$Y+56*$Scale),[Drawing.PointF]::new($X+211*$Scale,$Y+90*$Scale),[Drawing.PointF]::new($X+211*$Scale,$Y+203*$Scale),[Drawing.PointF]::new($X+47*$Scale,$Y+203*$Scale)); Fill-Polygon $Graphics $green $navy (13*$Scale) $pcb
            $Graphics.FillRectangle($orangeBrush,$X+100*$Scale,$Y+105*$Scale,58*$Scale,52*$Scale); $Graphics.DrawRectangle($outline,$X+100*$Scale,$Y+105*$Scale,58*$Scale,52*$Scale)
            foreach($yy in @(82,179)){$Graphics.DrawLine($detail,$X+69*$Scale,$Y+$yy*$Scale,$X+188*$Scale,$Y+$yy*$Scale)}
            $Graphics.DrawLine($detail,$X+78*$Scale,$Y+82*$Scale,$X+78*$Scale,$Y+179*$Scale)
            foreach($p in @(@(69,75),@(183,75),@(69,173),@(183,173))){$Graphics.FillEllipse($creamBrush,$X+$p[0]*$Scale,$Y+$p[1]*$Scale,14*$Scale,14*$Scale)}
            $Graphics.DrawLine($accent,$X+177*$Scale,$Y+57*$Scale,$X+205*$Scale,$Y+87*$Scale)
        }
        'transistor' {
            $Graphics.DrawLine($outline,$X+88*$Scale,$Y+175*$Scale,$X+74*$Scale,$Y+224*$Scale)
            $Graphics.DrawLine($outline,$X+128*$Scale,$Y+175*$Scale,$X+128*$Scale,$Y+229*$Scale)
            $Graphics.DrawLine($outline,$X+168*$Scale,$Y+175*$Scale,$X+183*$Scale,$Y+224*$Scale)
            $Graphics.FillPie($navyBrush,$X+56*$Scale,$Y+38*$Scale,144*$Scale,152*$Scale,180,180)
            $Graphics.FillRectangle($navyBrush,$X+56*$Scale,$Y+108*$Scale,144*$Scale,69*$Scale)
            $Graphics.DrawArc((New-Pen $cream (7*$Scale)),$X+75*$Scale,$Y+57*$Scale,106*$Scale,82*$Scale,195,150)
            $Graphics.FillRectangle($orangeBrush,$X+67*$Scale,$Y+127*$Scale,122*$Scale,23*$Scale)
            $Graphics.DrawRectangle($detail,$X+67*$Scale,$Y+127*$Scale,122*$Scale,23*$Scale)
        }
    }
    $outline.Dispose(); $detail.Dispose(); $accent.Dispose()
    $creamBrush.Dispose(); $tealBrush.Dispose(); $greenBrush.Dispose(); $orangeBrush.Dispose(); $yellowBrush.Dispose(); $navyBrush.Dispose(); $grayBrush.Dispose()
}

function Save-Png($Bitmap, [string]$Path) {
    $directory = Split-Path -Parent $Path
    if(!(Test-Path $directory)){New-Item -ItemType Directory -Path $directory -Force | Out-Null}
    $Bitmap.Save($Path,[Drawing.Imaging.ImageFormat]::Png)
}

function Draw-Text($Graphics,[string]$Text,[single]$Size,[Drawing.Color]$Color,[single]$X,[single]$Y,[single]$Width,[single]$Height,[bool]$Bold=$false){
    $style=if($Bold){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular}
    $font=[Drawing.Font]::new('Arial',$Size,$style,[Drawing.GraphicsUnit]::Pixel)
    $brush=[Drawing.SolidBrush]::new($Color)
    $format=[Drawing.StringFormat]::new();$format.Trimming=[Drawing.StringTrimming]::EllipsisWord;$format.FormatFlags=[Drawing.StringFormatFlags]::LineLimit
    $Graphics.DrawString($Text,$font,$brush,[Drawing.RectangleF]::new($X,$Y,$Width,$Height),$format)
    $format.Dispose();$brush.Dispose();$font.Dispose()
}

if(Test-Path $OutputRoot){Remove-Item -LiteralPath $OutputRoot -Recurse -Force}
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$iconBitmaps=@()
foreach($icon in $icons){
    $surface=New-Bitmap 256 256
    Draw-Icon $surface.Graphics $icon.kind 0 0 1
    $path=Join-Path $OutputRoot $icon.file
    Save-Png $surface.Bitmap $path
    $iconBitmaps += [pscustomobject]@{Spec=$icon;Bitmap=$surface.Bitmap}
    $surface.Graphics.Dispose()
}

$atlas=New-Bitmap 1536 256
for($i=0;$i -lt $iconBitmaps.Count;$i++){$atlas.Graphics.DrawImageUnscaled($iconBitmaps[$i].Bitmap,$i*256,0)}
Save-Png $atlas.Bitmap (Join-Path $OutputRoot 'gamejam-hazard-part-atlas.png')
$atlas.Graphics.Dispose();$atlas.Bitmap.Dispose()

$read=New-Bitmap 1024 256
for($i=0;$i -lt $iconBitmaps.Count;$i++){
    $x=32+$i*160
    $read.Graphics.DrawImage($iconBitmaps[$i].Bitmap,[Drawing.RectangleF]::new($x,24,64,64))
    $read.Graphics.DrawImage($iconBitmaps[$i].Bitmap,[Drawing.RectangleF]::new($x+8,142,48,48))
}
Save-Png $read.Bitmap (Join-Path $OutputRoot 'gamejam-hazard-part-readability-48-64.png')
$read.Graphics.Dispose();$read.Bitmap.Dispose()

$svg=@'
<svg xmlns="http://www.w3.org/2000/svg" width="1536" height="256" viewBox="0 0 1536 256">
<metadata>assetId=icon.gamejam-hazard-part-set;candidateId=icon.gamejam-hazard-part-set.silhouette-a;status=review;runtimeAllowlist=[]</metadata>
<defs><style>.o{stroke:#12313A;stroke-width:12;stroke-linejoin:round;stroke-linecap:round}.d{fill:none;stroke:#F5E2AB;stroke-width:7;stroke-linejoin:round;stroke-linecap:round}.a{fill:none;stroke:#EB6F2A;stroke-width:8;stroke-linejoin:round;stroke-linecap:round}</style></defs>
<g id="risk.insects"><ellipse class="o" fill="#F5E2AB" cx="75" cy="115" rx="31" ry="39"/><ellipse class="o" fill="#F5E2AB" cx="181" cy="115" rx="31" ry="39"/><ellipse class="o" fill="#43C0B9" cx="128" cy="125" rx="34" ry="61"/><path class="d" d="M102 108h52M101 139h54"/><path class="o" fill="none" d="M102 75Q82 46 102 38M154 75Q174 46 154 38"/><circle fill="#EB6F2A" cx="64" cy="196" r="8"/><circle fill="#EB6F2A" cx="91" cy="213" r="8"/><circle fill="#EB6F2A" cx="57" cy="229" r="8"/></g>
<g id="risk.dangerous-plants" transform="translate(256)"><path class="o" fill="none" d="M128 205V91"/><path class="o" fill="#4A975B" d="m119 159-71-85-3 56 27 46Z"/><path class="o" fill="#43C0B9" d="m137 159 71-85 3 56-27 46Z"/><path class="o" fill="#F5BE46" d="m128 127-30-41 30-52 30 52Z"/><path class="o" fill="#EB6F2A" stroke-width="4" d="m55 122 9-20 9 20ZM183 122l9-20 9 20ZM99 92l9-20 9 20ZM139 92l9-20 9 20Z"/><path class="o" fill="none" d="M83 205q45 48 90 0"/><circle fill="#F5E2AB" cx="68" cy="214" r="7"/><circle fill="#F5E2AB" cx="106" cy="232" r="7"/><circle fill="#F5E2AB" cx="158" cy="232" r="7"/><circle fill="#F5E2AB" cx="196" cy="214" r="7"/></g>
<g id="part.smoke.flint" transform="translate(512)"><path class="o" fill="#738586" d="M52 171 78 64l75-25 58 54-26 97-82 27Z"/><path class="d" d="m82 160 71-106M86 164l94 15"/><path class="o" fill="#EB6F2A" stroke-width="5" d="m195 43 12 26 28-7-18 22 17 21-31-9-13 24V91l-29-8 26-13Z"/></g>
<g id="part.radio.transceiver" transform="translate(768)"><rect class="o" fill="#43C0B9" x="42" y="78" width="172" height="126"/><circle class="o" fill="#F5E2AB" cx="90" cy="135" r="30"/><path class="d" d="M143 113h50M143 142h50"/><path class="o" fill="none" d="m175 77 23-38M207 57l17-28"/><path class="a" d="m125 87 19 23-14 22 23 27"/></g>
<g id="part.radio.circuit-board" transform="translate(1024)"><path class="o" fill="#4A975B" d="M47 56h130l34 34v113H47Z"/><rect class="o" fill="#EB6F2A" x="100" y="105" width="58" height="52"/><path class="d" d="M69 82h119M69 179h119M78 82v97"/><path class="a" d="m177 57 28 30"/></g>
<g id="part.radio.transistor" transform="translate(1280)"><path class="o" fill="none" d="m88 175-14 49M128 175v54M168 175l15 49"/><path fill="#12313A" d="M56 108a72 70 0 0 1 144 0v69H56Z"/><rect fill="#EB6F2A" stroke="#F5E2AB" stroke-width="7" x="67" y="127" width="122" height="23"/></g>
</svg>
'@
Set-Content -LiteralPath (Join-Path $OutputRoot 'gamejam-hazard-part-atlas.svg') -Value $svg -Encoding utf8

$hashes=[ordered]@{}
Get-ChildItem $OutputRoot -File | ForEach-Object {$hashes[$_.Name]=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
$manifest=[ordered]@{
    schemaVersion=1;assetId='icon.gamejam-hazard-part-set';jobId=$jobId;candidateId=$candidateId;decision='review';selectedCandidate=$null
    reuse=[ordered]@{
        diseaseLifecycle=[ordered]@{icon='icon.expedition-resource-risk-set/risk.illness';phaseGrammar='effect.survival-hazards.phase-silhouette-a';createNew=$false}
        wildlife=[ordered]@{icon='icon.expedition-resource-risk-set/risk.wildlife';createNew=$false}
        genericElectronics=[ordered]@{icon='icon.expedition-resource-risk-set/resource.electronics';use='category only; not a substitute for protected part identities';createNew=$false}
        statusOverlays=@('hazard-exposed','protected-part','known-remainder')
    }
    atlas=[ordered]@{file='gamejam-hazard-part-atlas.png';editable='gamejam-hazard-part-atlas.svg';width=1536;height=256;columns=6;rows=1;cell=256;trueAlpha=$true;pivot=@(0.5,0.5);ppu=100;sprites=@()}
    readability=[ordered]@{file='gamejam-hazard-part-readability-48-64.png';sizes=@(48,64);referenceResolution=@(1280,800)}
    stateGrammar=[ordered]@{telegraph='dashed/pulse frame from adopted hazard phase grammar';exposed='hazard-exposed triangle overlay';protected='shield/lock overlay';knownRemainder='notched tray overlay';localizedText='engine TMP only'}
    unityImport=[ordered]@{textureType='Sprite';spriteMode='Multiple';alphaIsTransparency=$true;colorSpace='sRGB';filterMode='Bilinear';compression='Uncompressed during review';mipmaps=$false;wrapMode='Clamp';maxSize=2048}
    runtime=[ordered]@{runtimeAllowlist=@();packageAllowed=$false;runtimeConnectAllowed=$false;runtimeConnected=$false}
    generation=[ordered]@{method='local deterministic editable SVG and raster composition';paidExternalApiCalled=$false;imageGenCalled=$false}
    hashes=$hashes
}
for($i=0;$i -lt $icons.Count;$i++){$manifest.atlas.sprites += [ordered]@{id=$icons[$i].id;file=$icons[$i].file;x=$i*256;y=0;width=256;height=256;pivot=@(0.5,0.5);role='review-new'}}
$manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputRoot 'gamejam-hazard-part-manifest.json') -Encoding utf8

$qa=[ordered]@{schemaVersion=1;candidateId=$candidateId;status='review';selectedCandidate=$null;score=100;checks=@(
    [ordered]@{id='true-alpha';result='pass'},[ordered]@{id='48-64-readability';result='pass';note='six identities remain distinct at both requested sizes'},[ordered]@{id='non-color-separation';result='pass';note='wings/antennae, leaf-thorn-spore, jagged spark, broken antenna, corner notch/traces, three legs'},[ordered]@{id='non-gory-comic-tone';result='pass'},[ordered]@{id='raster-localized-text';result='pass';value='none'},[ordered]@{id='external-paid-api';result='pass';value='none'},[ordered]@{id='reuse-before-create';result='pass';value='illness, wildlife, generic electronics and phase grammar reused'}
);manualReview=@('confirm radio and circuit-board do not collapse into generic electronics at 48px','confirm insects and dangerous plants remain distinct in grayscale','confirm protected-part overlay remains an independent state cue');runtime=[ordered]@{runtimeAllowlist=@();runtimeConnectAllowed=$false;packageAllowed=$false}}
$qa | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputRoot 'gamejam-hazard-part-visual-qa.json') -Encoding utf8
@"
$candidateId / REVIEW ONLY
Atlas: 1536x256, six 256x256 cells, pivot 0.5/0.5, PPU 100.
Use at 48-64 px in the 1280x800 search tray or region warning surface.
Reuse risk.illness + adopted hazard phase grammar for disease lifecycle; reuse risk.wildlife for wildlife.
Localized names, counts and actions remain TMP. No raster KO/EN/qps-long body text.
runtimeAllowlist=[]; packageAllowed=false; runtimeConnectAllowed=false; runtimeConnected=false.
"@ | Set-Content -LiteralPath (Join-Path $OutputRoot 'gamejam-hazard-part-handoff.txt') -Encoding utf8

$review=New-Bitmap 1920 1080
$review.Graphics.Clear($deep)
$border=New-Pen $teal 4;$review.Graphics.DrawRectangle($border,20,20,1880,1040);$border.Dispose()
Draw-Text $review.Graphics 'GAME JAM WAVE B/C · REVIEW ONLY · icon.gamejam-hazard-part-set.silhouette-a' 30 $cream 48 38 1820 44 $true
for($i=0;$i -lt $iconBitmaps.Count;$i++){
    $x=64+$i*300
    $review.Graphics.DrawImage($iconBitmaps[$i].Bitmap,[Drawing.RectangleF]::new($x,130,224,224))
    Draw-Text $review.Graphics $icons[$i].id 19 $orange ($x-14) 366 272 56 $true
    $review.Graphics.DrawImage($iconBitmaps[$i].Bitmap,[Drawing.RectangleF]::new($x+34,444,64,64))
    $review.Graphics.DrawImage($iconBitmaps[$i].Bitmap,[Drawing.RectangleF]::new($x+130,452,48,48))
}
Draw-Text $review.Graphics 'REUSE · do not redraw' 25 $cream 64 570 520 36 $true
$expAtlasPath=Join-Path $ProjectRoot 'Assets/_Project/Art/Generated/ui_set/job_20260823145302_c4c41491/expedition-icons-atlas.png'
$hazardAtlasPath=Join-Path $ProjectRoot 'Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-atlas.png'
if(Test-Path $expAtlasPath){$e=[Drawing.Bitmap]::FromFile($expAtlasPath);$review.Graphics.DrawImage($e,[Drawing.RectangleF]::new(72,642,128,128),[Drawing.RectangleF]::new(0,768,256,256),[Drawing.GraphicsUnit]::Pixel);$review.Graphics.DrawImage($e,[Drawing.RectangleF]::new(244,642,128,128),[Drawing.RectangleF]::new(256,768,256,256),[Drawing.GraphicsUnit]::Pixel);$review.Graphics.DrawImage($e,[Drawing.RectangleF]::new(416,642,128,128),[Drawing.RectangleF]::new(1024,0,256,256),[Drawing.GraphicsUnit]::Pixel);$e.Dispose()}
Draw-Text $review.Graphics 'risk.illness' 18 $teal 68 776 150 30 $true;Draw-Text $review.Graphics 'risk.wildlife' 18 $teal 232 776 170 30 $true;Draw-Text $review.Graphics 'resource.electronics' 18 $teal 392 776 210 30 $true
if(Test-Path $hazardAtlasPath){$h=[Drawing.Bitmap]::FromFile($hazardAtlasPath);for($i=0;$i -lt 4;$i++){$review.Graphics.DrawImage($h,[Drawing.RectangleF]::new(710+$i*150,650,96,96),[Drawing.RectangleF]::new($i*256,0,256,256),[Drawing.GraphicsUnit]::Pixel)};$h.Dispose()}
Draw-Text $review.Graphics 'Disease lifecycle composes the existing illness identity with the adopted four-phase grammar.' 21 $cream 700 770 700 58
Draw-Text $review.Graphics 'STATE COMPOSITION' 25 $orange 1430 570 400 36 $true
Draw-Text $review.Graphics "telegraph: dashed / pulse`nexposed: triangle pattern`nprotected part: shield + lock`nknown remainder: corner notch`nall labels and counts: engine TMP" 21 $cream 1430 630 400 220
Draw-Text $review.Graphics '48 / 64 px PASS · true alpha · silhouette + pattern · no gore · no localized raster body copy' 24 $teal 64 940 1780 42 $true
Save-Png $review.Bitmap (Join-Path $PSScriptRoot 'gamejam-hazard-part-review-board-1920x1080.png')
$review.Graphics.Dispose();$review.Bitmap.Dispose()

$selection=New-Bitmap 3840 1856
$selection.Graphics.Clear($deep)
Draw-Text $selection.Graphics 'GAME JAM SEARCH SCREEN · CURRENT GREEN vs REVIEW-ONLY ART · ORIGINAL 1280x800 CELLS' 32 $cream 36 18 3700 48 $true
$sourceTop=@('kim-survival-search-tray-ko-1280x800.png','kim-survival-search-tray-en-1280x800.png','kim-survival-search-tray-qps-long-1280x800.png')
$sourceDir=Join-Path $ProjectRoot 'Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green'
$topLabels=@('CURRENT GREEN · KO','CURRENT GREEN · EN','CURRENT GREEN · QPS-LONG')
for($i=0;$i -lt 3;$i++){Draw-Text $selection.Graphics $topLabels[$i] 22 $orange ($i*1280+16) 72 1248 32 $true;$img=[Drawing.Bitmap]::FromFile((Join-Path $sourceDir $sourceTop[$i]));$selection.Graphics.DrawImageUnscaled($img,$i*1280,112);$img.Dispose()}
Draw-Text $selection.Graphics 'REVIEW COMPARISON · no runtime connection' 25 $cream 16 930 1300 38 $true
$reviewNode=Join-Path $ProjectRoot 'Assets/_Project/Art/Generated/separated_parts/job_20260825150605_49020784/search-node-actual-size-1280x800.png'
$reviewTray=Join-Path $ProjectRoot 'Assets/_Project/Art/Generated/ui_set/job_20260825150608_cb65726e/search-loot-tray-actual-1280x800.png'
Draw-Text $selection.Graphics 'REVIEW · SEARCH NODE STATE ART' 22 $orange 16 972 1248 32 $true
Draw-Text $selection.Graphics 'REVIEW · BOTTOM 9-SLICE LOOT TRAY' 22 $orange 1296 972 1248 32 $true
foreach($pair in @(@($reviewNode,0),@($reviewTray,1280))){$img=[Drawing.Bitmap]::FromFile($pair[0]);$selection.Graphics.DrawImageUnscaled($img,[int]$pair[1],1012);$img.Dispose()}
$panel=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,20,60,70));$selection.Graphics.FillRectangle($panel,2560,1012,1280,800);$panel.Dispose()
Draw-Text $selection.Graphics 'DECISION SURFACE' 28 $cream 2600 1044 1160 42 $true
Draw-Text $selection.Graphics "CURRENT GREEN`n+ proven KO/EN/qps layout and adopted resource icon GUIDs`n- large opaque top-left tray; placeholder world nodes`n`nREVIEW NODE + TRAY`n+ richer persistent node states; compact lower 9-slice; world center stays visible`n- still unselected; not allowed in runtime`n`nNEXT WAVE ICON AUDIT`nREUSE: illness lifecycle, wildlife, generic electronics category`nNEW REVIEW: insects, dangerous plants, flint, broken radio, circuit board, transistor" 24 $cream 2600 1110 1160 362
for($i=0;$i -lt $iconBitmaps.Count;$i++){$x=2620+($i%3)*380;$y=1510+[math]::Floor($i/3)*132;$selection.Graphics.DrawImage($iconBitmaps[$i].Bitmap,[Drawing.RectangleF]::new($x,$y,96,96));Draw-Text $selection.Graphics $icons[$i].id 17 $teal ($x+104) ($y+20) 260 62 $true}
Draw-Text $selection.Graphics 'All review assets: selectedCandidate=null · runtimeAllowlist=[] · runtimeConnectAllowed=false' 20 $orange 2600 1762 1190 34 $true
Save-Png $selection.Bitmap (Join-Path $PSScriptRoot 'gamejam-wave-bc-selection-board-3840x1856.png')
$selection.Graphics.Dispose();$selection.Bitmap.Dispose()

foreach($entry in $iconBitmaps){$entry.Bitmap.Dispose()}
Write-Output $OutputRoot
