param([string]$Dll)

$mod = [Environment]::GetEnvironmentVariable("GENEFOUNDRY_MOD_DIR", "User")
Copy-Item $Dll "$mod\Assemblies\GeneFoundry.dll" -Force