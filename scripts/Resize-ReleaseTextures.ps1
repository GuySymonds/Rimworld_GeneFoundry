# Release-only helper. Call after copying fresh assets, never on a development installation.
function Resize-ReleaseTextures([string]$PackagePath) {
    Add-Type -AssemblyName System.Drawing
    $package = Assert-PlainModDirectory $PackagePath
    $textures = Join-Path $package 'Textures'
    $files = @(Get-ChildItem -LiteralPath $textures -Recurse -File -Filter '*.png' | Sort-Object FullName)
    $before = ($files | Measure-Object Length -Sum).Sum
    foreach ($file in $files) {
        $original = $bitmap = $graphics = $attributes = $null
        $temporary = $file.FullName + '.resizing'
        try {
            $original = [Drawing.Image]::FromFile($file.FullName)
            if ($original.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid) {
                throw 'Input is not a PNG.'
            }
            # Round half up, including odd dimensions; a one-pixel axis stays one pixel.
            $width = [int][Math]::Ceiling($original.Width / 2.0)
            $height = [int][Math]::Ceiling($original.Height / 2.0)
            $bitmap = [Drawing.Bitmap]::new($width, $height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics = [Drawing.Graphics]::FromImage($bitmap)
            $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $attributes = [Drawing.Imaging.ImageAttributes]::new()
            $attributes.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
            $graphics.DrawImage($original, [Drawing.Rectangle]::new(0, 0, $width, $height),
                0, 0, $original.Width, $original.Height, [Drawing.GraphicsUnit]::Pixel, $attributes)
            $bitmap.Save($temporary, [Drawing.Imaging.ImageFormat]::Png)
            $original.Dispose()
            $original = $null
            Move-Item -LiteralPath $temporary -Destination $file.FullName -Force
        }
        catch { throw "Failed resizing release texture '$($file.FullName)': $_" }
        finally {
            foreach ($resource in @($attributes, $graphics, $bitmap, $original)) {
                if ($null -ne $resource) { $resource.Dispose() }
            }
            if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
        }
    }
    $after = (Get-ChildItem -LiteralPath $textures -Recurse -File -Filter '*.png' | Measure-Object Length -Sum).Sum
    Write-Host ("Release textures: {0} PNGs; {1} -> {2} bytes ({3:N2}% smaller)." -f $files.Count, $before, $after, (100 * (1 - $after / $before)))
}
