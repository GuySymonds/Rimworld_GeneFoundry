param(
    [Parameter(Mandatory = $true)][string]$Dll,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repo 'Mod'
$mod = $env:GENEFOUNDRY_MOD_DIR
if (-not $mod) { $mod = [Environment]::GetEnvironmentVariable('GENEFOUNDRY_MOD_DIR', 'User') }
if (-not $mod) {
    throw 'Set GENEFOUNDRY_MOD_DIR to the local Mods/GeneFoundry directory (it need not exist yet).'
}
$mod = Assert-PlainModDirectory $mod
if ((Split-Path $mod -Leaf) -ne 'GeneFoundry') {
    throw 'GENEFOUNDRY_MOD_DIR must point to the local Mods/GeneFoundry directory.'
}
if ($Configuration -eq 'Debug') { $mod = Assert-PlainModDirectory ($mod + '_dev') }
$metadata = Join-Path $mod 'About/About.xml'
[xml]$expected = Get-Content -LiteralPath (Join-Path $source 'About/About.xml') -Raw
if (Test-Path -LiteralPath $metadata -PathType Leaf) {
    [xml]$installed = Get-Content -LiteralPath $metadata -Raw
    if ($installed.ModMetaData.packageId -ne $expected.ModMetaData.packageId) {
        throw 'Deployment target package ID does not match GeneFoundry.'
    }
}
elseif ((Test-Path -LiteralPath $mod) -and @(Get-ChildItem -LiteralPath $mod -Force).Count -gt 0) {
    throw 'Refusing nonempty deployment target without About/About.xml.'
}
Sync-GeneFoundryContent -Source $source -Destination $mod -Dll $Dll
Write-Host "GeneFoundry deployed from Mod/ to $mod (Workshop publication ID preserved)."
