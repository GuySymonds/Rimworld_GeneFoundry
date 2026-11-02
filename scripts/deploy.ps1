param([Parameter(Mandatory = $true)][string]$Dll)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repo 'Mod'
$mod = $env:GENEFOUNDRY_MOD_DIR
if (-not $mod) { $mod = [Environment]::GetEnvironmentVariable('GENEFOUNDRY_MOD_DIR', 'User') }
if (-not $mod -or -not (Test-Path -LiteralPath (Join-Path $mod 'About/About.xml') -PathType Leaf)) {
    throw 'Set GENEFOUNDRY_MOD_DIR to an existing local GeneFoundry mod directory containing About/About.xml.'
}
$mod = Assert-PlainModDirectory $mod
[xml]$installed = Get-Content -LiteralPath (Join-Path $mod 'About/About.xml') -Raw
[xml]$expected = Get-Content -LiteralPath (Join-Path $source 'About/About.xml') -Raw
if ($installed.ModMetaData.packageId -ne $expected.ModMetaData.packageId) {
    throw 'Deployment target package ID does not match GeneFoundry.'
}
Sync-GeneFoundryContent -Source $source -Destination $mod -Dll $Dll
Write-Host "GeneFoundry deployed from Mod/ to $mod (Workshop publication ID preserved)."
