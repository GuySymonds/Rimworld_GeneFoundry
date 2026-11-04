[CmdletBinding()]
param([string]$PackagePath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
$repo = Split-Path $PSScriptRoot -Parent
if (-not $PackagePath) { $PackagePath = Join-Path $repo 'dist/GeneFoundry' }
$source = Join-Path $repo 'Mod'
$package = Assert-PlainModDirectory $PackagePath
function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Invalid release package: $Message" }
}
foreach ($file in @('About/About.xml', 'About/Preview.png', 'Assemblies/GeneFoundry.dll', 'LICENSE')) {
    $path = Join-Path $package $file
    Require (Test-Path -LiteralPath $path -PathType Leaf) "Missing $file"
    Require ((Get-Item -LiteralPath $path).Length -gt 0) "Empty $file"
}
[xml]$about = Get-Content -LiteralPath (Join-Path $package 'About/About.xml') -Raw
$meta = $about.ModMetaData
Require ($null -ne $meta) 'Expected ModMetaData root.'
Require ($meta.name -eq 'GeneFoundry') 'Unexpected mod name.'
Require ($meta.author -eq 'Guy Symonds') 'Unexpected author.'
Require ($meta.packageId -eq 'GBF.GeneticBiomassFabrication') 'Established package ID changed.'
Require ($meta.url -eq 'https://github.com/GuySymonds/Rimworld_GeneFoundry') 'Repository URL missing.'
Require (@($meta.supportedVersions.li).Count -eq 1 -and $meta.supportedVersions.li -eq '1.6') 'Only RimWorld 1.6 is supported.'
Require (@($meta.modDependencies.li | Where-Object { $_.packageId -eq 'Ludeon.RimWorld.Biotech' -and $_.displayName -eq 'Biotech' }).Count -eq 1) 'Biotech dependency missing.'
Require (-not [string]::IsNullOrWhiteSpace($meta.description)) 'Description missing.'
$version = [string]$meta.modVersion
Require ($version -match '^\d+\.\d+\.\d+$') 'Expected semantic version major.minor.patch.'
$assemblyPath = Join-Path $package 'Assemblies/GeneFoundry.dll'
$assembly = [Reflection.AssemblyName]::GetAssemblyName($assemblyPath)
$fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($assemblyPath)
Require ($assembly.Name -eq 'GeneFoundry' -and $assembly.Version.ToString() -eq "$version.0") 'Assembly version differs from About.xml.'
Require ($fileVersion.FileVersion -eq "$version.0" -and $fileVersion.ProductVersion -eq $version) 'File/informational version differs from About.xml.'

Add-Type -AssemblyName System.Drawing
foreach ($art in @(@('Preview.png', 640, 360), @('ModIcon.png', 64, 64))) {
    $path = Join-Path $package "About/$($art[0])"
    if ($art[0] -eq 'ModIcon.png' -and -not (Test-Path -LiteralPath $path)) { continue }
    $bitmap = [Drawing.Image]::FromFile($path)
    try {
        Require ($bitmap.Width -eq $art[1] -and $bitmap.Height -eq $art[2]) "Wrong dimensions: $($art[0])"
        Require ($bitmap.RawFormat.Guid -eq [Drawing.Imaging.ImageFormat]::Png.Guid) "Not PNG: $($art[0])"
        Require ((Get-Item -LiteralPath $path).Length -lt 1000000) "Artwork exceeds 1 MB: $($art[0])"
        # The preview has an opaque background; mod icons may use transparency.
        if ($art[0] -eq 'Preview.png') {
            foreach ($point in @(@(0, 0), @(10, 10), @(32, 32))) {
                Require ($bitmap.GetPixel($point[0], $point[1]).A -eq 255) "Unexpected transparency: $($art[0])"
            }
        }
    }
    finally { $bitmap.Dispose() }
}

# Exact file allowlist: no source art, source code, tests, symbols or development docs.
$expected = @{}
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File) {
    $relative = $file.FullName.Substring($source.Length + 1).Replace('\', '/')
    Require ($relative -match '^(About/(About\.xml|Preview\.png|ModIcon\.png|PublishedFileId\.txt)|LICENSE|Defs/.+\.xml|Languages/.+\.xml|Patches/.+\.xml|Textures/.+\.png)$') "Development/unexpected file in Mod/: $relative"
    if ($relative -eq 'About/PublishedFileId.txt') { continue }
    $expected[$relative] = $true
    $target = Join-Path $package $relative
    Require (Test-Path -LiteralPath $target -PathType Leaf) "Missing runtime file: $relative"
    Require ((Get-FileHash -LiteralPath $target).Hash -eq (Get-FileHash -LiteralPath $file.FullName).Hash) "Stale/changed runtime file: $relative"
}
$expected['Assemblies/GeneFoundry.dll'] = $true
foreach ($file in Get-ChildItem -LiteralPath $package -Recurse -File -Force) {
    $relative = $file.FullName.Substring($package.Length + 1).Replace('\', '/')
    if ($relative -eq 'About/PublishedFileId.txt') {
        Require ((Get-Content -LiteralPath $file.FullName -Raw).Trim() -match '^[1-9][0-9]*$') 'Invalid Workshop publication ID.'
    }
    else { Require ($expected.ContainsKey($relative)) "Unexpected file: $relative" }
}
foreach ($directory in Get-ChildItem -LiteralPath $package -Directory -Force) {
    Require ($directory.Name -in @('About', 'Assemblies', 'Defs', 'Languages', 'Textures', 'Patches')) "Unexpected directory: $($directory.Name)"
}
Require ((Get-FileHash -LiteralPath (Join-Path $package 'LICENSE')).Hash -eq (Get-FileHash -LiteralPath (Join-Path $repo 'LICENSE')).Hash) 'Runtime LICENSE differs from repository LICENSE.'

$definitions = @(Get-ChildItem -LiteralPath (Join-Path $package 'Defs') -Filter '*.xml' -Recurse)
Require ($definitions.Count -gt 0) 'No production definitions.'
foreach ($file in $definitions) {
    [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw
    Require ($xml.DocumentElement.Name -eq 'Defs') "Wrong definition root: $($file.Name)"
    foreach ($texture in $xml.SelectNodes('//texPath[starts-with(text(),"GeneFoundry/")]')) {
        $relative = $texture.InnerText
        Require ($relative -notmatch '\.\.|\\') "Unsafe texture path: $relative"
        $path = Join-Path $package "Textures/$relative"
        $graphic = $texture.ParentNode.graphicClass
        if ($graphic -eq 'GeneFoundry.Recipes.Graphic_GeneVat') {
            # Native Graphic_Multi falls back to north for south and mirrors east for west.
            foreach ($direction in @('north', 'east')) {
                Require (Test-Path -LiteralPath "${path}_${direction}.png") "Missing directional vat texture: ${relative}_${direction}"
            }
        }
        elseif ($graphic -in @('Graphic_Multi', 'GeneFoundry.Fabrication.Graphic_GeneFabricator')) {
            foreach ($direction in @('north', 'east', 'south', 'west')) {
                Require (Test-Path -LiteralPath "${path}_${direction}.png") "Missing directional texture: ${relative}_${direction}"
            }
        }
        elseif ($graphic -eq 'Graphic_StackCount') {
            Require ((Test-Path -LiteralPath $path -PathType Container) -and
                @(Get-ChildItem -LiteralPath $path -Filter '*.png').Count -gt 0) "Missing stack textures: $relative"
        }
        else { Require (Test-Path -LiteralPath "$path.png" -PathType Leaf) "Missing texture: $relative" }
    }
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $package 'Languages') -Filter '*.xml' -Recurse) {
    [xml]$translation = Get-Content -LiteralPath $file.FullName -Raw
    Require ($translation.DocumentElement.Name -eq 'LanguageData') "Wrong translation root: $($file.Name)"
}
for ($frame = 0; $frame -lt 8; $frame++) {
    foreach ($direction in @('north', 'east', 'south', 'west')) {
        $relative = 'Textures/GeneFoundry/Buildings/GeneVatFrames/vat_{0:00}_{1}.png' -f $frame, $direction
        $framePath = Join-Path $package $relative
        Require (Test-Path -LiteralPath $framePath) "Missing animation frame: $relative"
        $basePath = Join-Path $package "Textures/GeneFoundry/Buildings/GeneVat_$direction.png"
        Require (Test-Path -LiteralPath $basePath) "Missing directional base: $direction"
        $frameImage = [Drawing.Image]::FromFile($framePath)
        $baseImage = [Drawing.Image]::FromFile($basePath)
        try {
            Require ($frameImage.Size -eq $baseImage.Size) "Frame canvas differs from $direction base: $relative"
        }
        finally { $frameImage.Dispose(); $baseImage.Dispose() }
    }
    $bubble = 'Textures/GeneFoundry/Effects/BubbleMass/{0:00}.png' -f $frame
    Require (Test-Path -LiteralPath (Join-Path $package $bubble)) "Missing animation frame: $bubble"
}
foreach ($direction in @('north', 'east', 'south', 'west')) {
    $fabricatorFiles = @("GeneFabricationDefault_$direction") + @(0..7 | ForEach-Object { "GeneFabrication_Frame${_}_$direction" })
    foreach ($name in $fabricatorFiles) {
        $relative = "Textures/GeneFoundry/Buildings/GeneFabricatorFrames/$name.png"
        Require (Test-Path -LiteralPath (Join-Path $package $relative)) "Missing fabricator frame: $relative"
    }
    foreach ($frame in 0..7) {
        $relative = 'Textures/GeneFoundry/Buildings/BiomassRefineryFrames/prep{0:00}_{1}.png' -f $frame, $direction
        Require (Test-Path -LiteralPath (Join-Path $package $relative)) "Missing refinery preparation frame: $relative"
    }
}
$refineryNames = @('CleanVat', 'DirtyVat') + @(0..7 | ForEach-Object { 'VatFilling{0:00}' -f $_ }) + @(0..15 | ForEach-Object { 'VatProcess{0:00}' -f $_ })
foreach ($name in $refineryNames) {
    $relative = "Textures/GeneFoundry/Buildings/BiomassRefineryFrames/$name.png"
    Require (Test-Path -LiteralPath (Join-Path $package $relative)) "Missing refinery overlay: $relative"
}
Write-Host "Validated GeneFoundry v${version}: $($expected.Count) runtime files, $($definitions.Count) definition files, assembly, metadata, textures and artwork."
