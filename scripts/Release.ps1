[CmdletBinding()]
param([string]$RimWorldManagedDir = $env:RimWorldManagedDir)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
. (Join-Path $PSScriptRoot 'Resize-ReleaseTextures.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$source = Assert-PlainModDirectory (Join-Path $repo 'Mod')
$dist = Assert-PlainModDirectory (Join-Path $repo 'dist')
$package = Assert-PlainModDirectory (Join-Path $dist 'GeneFoundry')

Assert-WorkshopPublicationIdentity -Source $source -Destination $package | Out-Null

# Packaging must not deploy, even when GENEFOUNDRY_MOD_DIR is set.
$arguments = @('build', (Join-Path $repo 'Source/GeneFoundry/GeneFoundry/GeneFoundry.csproj'),
    '-c', 'Release', '-p:DeployMod=false', '-p:PostBuildEvent=', '--verbosity', 'minimal')
if ($RimWorldManagedDir) { $arguments += "-p:RimWorldManagedDir=$RimWorldManagedDir" }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Release build failed; package was not changed.' }
$dll = Join-Path $repo 'Source/GeneFoundry/GeneFoundry/bin/Release/GeneFoundry.dll'

# The shared copier validates inputs and preserves the Workshop ID before cleanup.
Sync-GeneFoundryContent -Source $source -Destination $package -Dll $dll
Resize-ReleaseTextures -PackagePath $package
& (Join-Path $PSScriptRoot 'Test-ReleasePackage.ps1') -PackagePath $package
Write-Host "Workshop-ready package: $package"
