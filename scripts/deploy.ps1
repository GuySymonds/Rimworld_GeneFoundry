param([string]$Dll)

$ErrorActionPreference = "Stop"

# Locations
$repo = Split-Path $PSScriptRoot -Parent
$mod = [Environment]::GetEnvironmentVariable("GENEFOUNDRY_MOD_DIR", "User")

# Validate paths before deleting anything
if (-not $mod -or -not (Test-Path "$mod\About\About.xml")) {
    throw "Invalid GeneFoundry mod directory."
}

if (-not (Test-Path $Dll -PathType Leaf)) {
    throw "Compiled DLL not found: $Dll"
}

# Clean existing deployment directories
foreach ($folder in @("Assemblies", "Defs", "Textures", "Patches")) {
    $target = Join-Path $mod $folder

    if (Test-Path $target) {
        Remove-Item $target -Recurse -Force
    }
}

# Deploy compiled assembly
New-Item "$mod\Assemblies" -ItemType Directory -Force | Out-Null
Copy-Item $Dll "$mod\Assemblies\GeneFoundry.dll" -Force

# Deploy XML definitions, textures and patches
foreach ($folder in @("Defs", "Textures", "Patches")) {
    $source = Join-Path $repo $folder
    $target = Join-Path $mod $folder

    if (Test-Path $source) {
        New-Item $target -ItemType Directory -Force | Out-Null
        Copy-Item "$source\*" $target -Recurse -Force
    }
}

Write-Host "GeneFoundry deployed successfully."