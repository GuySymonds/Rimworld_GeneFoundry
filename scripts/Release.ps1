[CmdletBinding()]
param([string]$RimWorldManagedDir = $env:RimWorldManagedDir)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repo 'Mod'
$dist = Assert-PlainModDirectory (Join-Path $repo 'dist')
$package = Assert-PlainModDirectory (Join-Path $dist 'GeneFoundry')

# Packaging must not deploy, even when GENEFOUNDRY_MOD_DIR is set.
$arguments = @('build', (Join-Path $repo 'Source/GeneFoundry/GeneFoundry/GeneFoundry.csproj'),
    '-c', 'Release', '-p:DeployMod=false', '-p:PostBuildEvent=', '--verbosity', 'minimal')
if ($RimWorldManagedDir) { $arguments += "-p:RimWorldManagedDir=$RimWorldManagedDir" }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Release build failed; package was not changed.' }
$dll = Join-Path $repo 'Source/GeneFoundry/GeneFoundry/bin/Release/GeneFoundry.dll'

# Fixed staging directory only; preserve the ID in place even across interruptions.
if (Test-Path -LiteralPath $package) {
    foreach ($item in Get-ChildItem -LiteralPath $package -Force) {
        if ($item.Name -eq 'About') {
            foreach ($aboutItem in Get-ChildItem -LiteralPath $item.FullName -Force) {
                if ($aboutItem.Name -ne 'PublishedFileId.txt') {
                    if (-not $aboutItem.FullName.StartsWith($package + [IO.Path]::DirectorySeparatorChar,
                        [StringComparison]::OrdinalIgnoreCase)) { throw 'Cleanup escaped staging.' }
                    Remove-Item -LiteralPath $aboutItem.FullName -Recurse -Force
                }
            }
        }
        else {
            if (-not $item.FullName.StartsWith($package + [IO.Path]::DirectorySeparatorChar,
                [StringComparison]::OrdinalIgnoreCase)) { throw 'Cleanup escaped staging.' }
            Remove-Item -LiteralPath $item.FullName -Recurse -Force
        }
    }
}
Sync-GeneFoundryContent -Source $source -Destination $package -Dll $dll
& (Join-Path $PSScriptRoot 'Test-ReleasePackage.ps1') -PackagePath $package
Write-Host "Workshop-ready package: $package"
