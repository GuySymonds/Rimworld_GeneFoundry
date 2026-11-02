# Explicit exports only: never copy GIMP projects or the entire Source/Textures tree.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$source = Assert-PlainModDirectory (Join-Path $repo 'Source/Textures/GeneVatFrames')
$target = Assert-PlainModDirectory (Join-Path $repo 'Mod/Textures/GeneFoundry/Buildings')
$exports = @{ 'GeneVat_north.png' = 'GeneVat_north.png'; 'GeneVat_east.png' = 'GeneVat_east.png' }
foreach ($direction in @('south', 'west')) {
    $name = "GeneVat_$direction.png"
    if (Test-Path -LiteralPath (Join-Path $source $name)) { $exports[$name] = $name }
}
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
# Remove stale exports, allowing Graphic_Multi's native south/west fallbacks.
foreach ($name in @('GeneVat.png', 'GeneVat_south.png', 'GeneVat_west.png')) {
    if (-not $exports.ContainsKey($name)) {
        $obsolete = Join-Path $target $name
        if (Test-Path -LiteralPath $obsolete) { Remove-Item -LiteralPath $obsolete -Force }
    }
}
Write-Host 'Updated directional Gene Vat artwork and eight shared animation PNGs.'
