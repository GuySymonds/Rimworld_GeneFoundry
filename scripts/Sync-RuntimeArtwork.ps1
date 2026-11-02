# Explicit exports only: never copy GIMP projects or the entire Source/Textures tree.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$source = Assert-PlainModDirectory (Join-Path $repo 'Source/Textures/GeneVatFrames')
$target = Assert-PlainModDirectory (Join-Path $repo 'Mod/Textures/GeneFoundry/Buildings')
$exports = @{ 'GeneVat_north.png' = 'GeneVat.png' }
for ($frame = 0; $frame -lt 8; $frame++) {
    $name = 'vat_{0:00}.png' -f $frame
    $exports[$name] = "GeneVatFrames/$name"
}
foreach ($name in $exports.Keys) {
    if (-not (Test-Path -LiteralPath (Join-Path $source $name) -PathType Leaf)) {
        throw "Missing exported Gene Vat artwork: $name"
    }
}
foreach ($name in $exports.Keys) {
    $destination = Join-Path $target $exports[$name]
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $source $name) -Destination $destination -Force
}
# One rotating sprite replaces the obsolete directional runtime exports.
foreach ($direction in @('north', 'east', 'south', 'west')) {
    $obsolete = Join-Path $target "GeneVat_$direction.png"
    if (Test-Path -LiteralPath $obsolete) { Remove-Item -LiteralPath $obsolete -Force }
}
Write-Host 'Updated Gene Vat runtime sprite and eight animation PNGs from source exports.'
