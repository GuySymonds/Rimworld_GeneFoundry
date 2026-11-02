# Recompose only Workshop assets. Runtime sprites are read-only inputs.
# Windows PowerShell 5.1 / PowerShell 7 on Windows; uses built-in System.Drawing.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$repo = Split-Path $PSScriptRoot -Parent
$about = Join-Path $repo 'Mod/About'
New-Item -ItemType Directory -Path $about -Force | Out-Null

function Brush([string]$hex) {
    [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex))
}
function Text([Drawing.Graphics]$g, [string]$value, [float]$x, [float]$y,
    [float]$size, [string]$color, [Drawing.FontStyle]$style = 'Regular') {
    $font = [Drawing.Font]::new('Segoe UI', $size, $style, [Drawing.GraphicsUnit]::Pixel)
    $brush = Brush $color
    try { $g.DrawString($value, $font, $brush, $x, $y) }
    finally { $font.Dispose(); $brush.Dispose() }
}
function Sprite([Drawing.Graphics]$g, [string]$relative, [float]$x, [float]$y,
    [float]$width, [float]$height) {
    $asset = [Drawing.Image]::FromFile((Join-Path $repo "Mod/Textures/GeneFoundry/$relative"))
    try { $g.DrawImage($asset, [Drawing.RectangleF]::new($x, $y, $width, $height)) }
    finally { $asset.Dispose() }
}
function Save-Scaled([Drawing.Bitmap]$canvas, [int]$width, [int]$height, [string]$path) {
    $output = [Drawing.Bitmap]::new($width, $height)
    $g = [Drawing.Graphics]::FromImage($output)
    try {
        $g.Clear([Drawing.ColorTranslator]::FromHtml('#111719'))
        $g.InterpolationMode = 'HighQualityBicubic'
        $g.DrawImage($canvas, 0, 0, $width, $height)
        $output.Save($path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $g.Dispose(); $output.Dispose() }
}

$canvas = [Drawing.Bitmap]::new(1280, 720)
$g = [Drawing.Graphics]::FromImage($canvas)
try {
    $g.ScaleTransform(2, 2)
    $g.SmoothingMode = 'AntiAlias'
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $background = [Drawing.Drawing2D.LinearGradientBrush]::new(
        [Drawing.Rectangle]::new(0, 0, 640, 360),
        [Drawing.ColorTranslator]::FromHtml('#252D2B'),
        [Drawing.ColorTranslator]::FromHtml('#111719'), 30)
    $g.FillRectangle($background, 0, 0, 640, 360)
    $background.Dispose()

    $grid = [Drawing.Pen]::new([Drawing.Color]::FromArgb(18, 179, 203, 176), .5)
    for ($x = 0; $x -lt 640; $x += 24) { $g.DrawLine($grid, $x, 125, $x, 325) }
    for ($y = 125; $y -lt 325; $y += 24) { $g.DrawLine($grid, 0, $y, 640, $y) }
    $grid.Dispose()
    $accent = Brush '#ACD85B'
    $g.FillRectangle($accent, 28, 20, 3, 12)
    Text $g 'RIMWORLD 1.6  /  BIOTECH' 39 17 11 '#B9C5B6' 'Bold'
    Text $g 'GENEFOUNDRY' 26 38 48 '#080E0F' 'Bold'
    Text $g 'GENEFOUNDRY' 25 36 48 '#F3F0E4' 'Bold'
    Text $g 'ORGANIC MATTER. GENETIC POSSIBILITIES.' 30 98 12 '#ACD85B' 'Bold'

    # Restrained pools of light tie the original green/blue sprites into the layout.
    foreach ($glow in @(@(78, 222, 110, 158, 48, 96, 135, 36), @(465, 235, 240, 85, 32, 55, 116, 128))) {
        $brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb($glow[4], $glow[5], $glow[6], $glow[7]))
        $g.FillEllipse($brush, ($glow[0] - $glow[2] / 2), ($glow[1] - $glow[3] / 2), $glow[2], $glow[3])
        $brush.Dispose()
    }
    $shadow = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(90, 0, 0, 0))
    $g.FillEllipse($shadow, 33, 267, 128, 20)
    $g.FillEllipse($shadow, 187, 257, 165, 24)
    $g.FillEllipse($shadow, 367, 253, 242, 28)
    $shadow.Dispose()
    Sprite $g 'Buildings/GeneVat_north.png' 37 125 117 156
    Sprite $g 'Buildings/BiomassRefinery.png' 185 163 166 111
    Sprite $g 'Buildings/GeneFabrication_north.png' 369 176 240 96
    Text $g '01' 29 291 10 '#ACD85B' 'Bold'
    Text $g 'EXTRACT' 46 287 15 '#EEECE0' 'Bold'
    Text $g 'Meat, plants & corpses' 29 308 10 '#AEBAB6'
    Text $g '02' 213 291 10 '#ACD85B' 'Bold'
    Text $g 'REFINE' 230 287 15 '#EEECE0' 'Bold'
    Text $g 'Hopper-fed processing' 213 308 10 '#AEBAB6'
    Text $g '03' 423 291 10 '#ACD85B' 'Bold'
    Text $g 'FABRICATE' 440 287 15 '#EEECE0' 'Bold'
    Text $g 'Choose your genepacks' 423 308 10 '#AEBAB6'
    $line = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#46534A'), 1)
    $g.DrawLine($line, 28, 334, 612, 334)
    $line.Dispose()
    Text $g 'INDUSTRIAL BIOTECHNOLOGY' 28 340 8 '#82968A' 'Bold'
    Text $g 'BY GUY SYMONDS' 532 340 8 '#82968A'
    $g.FillRectangle($accent, 609, 20, 3, 12)
    $accent.Dispose()
    Save-Scaled $canvas 640 360 (Join-Path $about 'Preview.png')
}
finally { $g.Dispose(); $canvas.Dispose() }

$icon = [Drawing.Bitmap]::new(256, 256)
$g = [Drawing.Graphics]::FromImage($icon)
try {
    $g.ScaleTransform(4, 4)
    $g.Clear([Drawing.ColorTranslator]::FromHtml('#18211F'))
    $g.InterpolationMode = 'HighQualityBicubic'
    Sprite $g 'Buildings/GeneVat_north.png' 11 4 42 56
    $frame = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#ACD85B'), 1)
    $g.DrawRectangle($frame, 1, 1, 61, 61)
    $frame.Dispose()
    Save-Scaled $icon 64 64 (Join-Path $about 'ModIcon.png')
}
finally { $g.Dispose(); $icon.Dispose() }
Write-Host 'Created Mod/About/Preview.png (640x360) and Mod/About/ModIcon.png (64x64).'
