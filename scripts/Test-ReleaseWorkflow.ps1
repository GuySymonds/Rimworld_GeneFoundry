# Deterministic safety/regression checks against a disposable copy, never an installed mod.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Assert-PlainModDirectory (Join-Path $tempRoot ('GeneFoundry-ReleaseWorkflow-' + [Guid]::NewGuid().ToString('N')))
$target = Join-Path $fixture 'GeneFoundry'
$dll = Join-Path $repo 'dist/GeneFoundry/Assemblies/GeneFoundry.dll'
$validator = Join-Path $PSScriptRoot 'Test-ReleasePackage.ps1'
if (-not (Test-Path -LiteralPath $dll)) { throw 'Run scripts/Release.ps1 first.' }
try {
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
$sentinel = Join-Path $fixture 'outside-mod.txt'
Set-Content -LiteralPath $sentinel -Value 'Do not change'
$source = Join-Path $repo 'Mod'
Sync-GeneFoundryContent -Source $source -Destination $target -Dll $dll
& $validator -PackagePath $target

function Expect-Rejection([string]$Name, [scriptblock]$Action) {
    $rejected = $false
    try { & $Action } catch { $rejected = $true }
    if (-not $rejected) { throw "Expected rejection: $Name" }
    Write-Host "PASS: rejects $Name"
}
$id = Join-Path $target 'About/PublishedFileId.txt'
if (-not (Test-Path -LiteralPath $id)) { Set-Content -LiteralPath $id -Value '123456789' }
$idBefore = [IO.File]::ReadAllBytes($id)
$stale = Join-Path $target 'Defs/obsolete.xml'
Set-Content -LiteralPath $stale -Value '<Defs />'
$staleRoot = Join-Path $target 'old-local-file.txt'
$staleAbout = Join-Path $target 'About/obsolete.png'
Set-Content -LiteralPath $staleRoot -Value 'stale'
Set-Content -LiteralPath $staleAbout -Value 'stale'
Sync-GeneFoundryContent -Source $source -Destination $target -Dll $dll
foreach ($path in @($stale, $staleRoot, $staleAbout)) {
    if (Test-Path -LiteralPath $path) { throw "Stale file survived deployment: $path" }
}
if ([Convert]::ToBase64String($idBefore) -ne [Convert]::ToBase64String([IO.File]::ReadAllBytes($id))) {
    throw 'Deployment modified the Workshop publication ID.'
}
if ((Get-Content -LiteralPath $sentinel).Trim() -ne 'Do not change') { throw 'Deployment touched a sibling.' }
Write-Host 'PASS: publication ID and sibling preserved; obsolete definition removed.'

# Exercise the actual deployment entry point with isolated configuration targets.
$previousModDir = $env:GENEFOUNDRY_MOD_DIR
try {
    $env:GENEFOUNDRY_MOD_DIR = Join-Path $fixture 'Deployment/GeneFoundry'
    $devTarget = $env:GENEFOUNDRY_MOD_DIR + '_dev'
    $deploy = Join-Path $PSScriptRoot 'Deploy-Mod.ps1'
    & $deploy -Dll $dll -Configuration Debug
    if (-not (Test-Path -LiteralPath (Join-Path $devTarget 'Assemblies/GeneFoundry.dll')) -or
        (Test-Path -LiteralPath $env:GENEFOUNDRY_MOD_DIR)) { throw 'Debug deployment targeted the release folder.' }
    New-Item -ItemType Directory -Path $env:GENEFOUNDRY_MOD_DIR -Force | Out-Null
    $devMarker = Join-Path $devTarget 'debug-only.txt'
    Set-Content -LiteralPath $devMarker -Value 'Keep during Release deployment'
    & $deploy -Dll $dll -Configuration Release
    if (-not (Test-Path -LiteralPath (Join-Path $env:GENEFOUNDRY_MOD_DIR 'Assemblies/GeneFoundry.dll')) -or
        -not (Test-Path -LiteralPath $devMarker)) { throw 'Release deployment touched the debug folder.' }
    Remove-Item -LiteralPath (Join-Path $env:GENEFOUNDRY_MOD_DIR 'About/About.xml')
    Expect-Rejection 'nonempty deployment without metadata' { & $deploy -Dll $dll -Configuration Release }
    if (-not (Test-Path -LiteralPath (Join-Path $env:GENEFOUNDRY_MOD_DIR 'Assemblies/GeneFoundry.dll'))) {
        throw 'Rejected deployment removed installed content.'
    }
    Copy-Item -LiteralPath (Join-Path $source 'About/About.xml') -Destination (Join-Path $env:GENEFOUNDRY_MOD_DIR 'About/About.xml')
    $wrongMetadata = Join-Path $env:GENEFOUNDRY_MOD_DIR 'About/About.xml'
    (Get-Content -LiteralPath $wrongMetadata -Raw).Replace('GBF.GeneticBiomassFabrication', 'Other.Mod') |
        Set-Content -LiteralPath $wrongMetadata
    Expect-Rejection 'deployment over another package' { & $deploy -Dll $dll -Configuration Release }
    Write-Host 'PASS: missing Debug target, empty Release target, configuration isolation and deployment guards.'
}
finally { $env:GENEFOUNDRY_MOD_DIR = $previousModDir }

$stray = Join-Path $target 'Assemblies/GeneFoundry.pdb'
Set-Content -LiteralPath $stray -Value 'debug symbols'
Expect-Rejection 'debug symbols' { & $validator -PackagePath $target }
Remove-Item -LiteralPath $stray
$texture = Join-Path $target 'Textures/GeneFoundry/Buildings/GeneVat_north.png'
$textureBackup = Join-Path $fixture 'texture-backup.png'
Move-Item -LiteralPath $texture -Destination $textureBackup
try { Expect-Rejection 'missing runtime texture' { & $validator -PackagePath $target } }
finally { Move-Item -LiteralPath $textureBackup -Destination $texture }
$about = Join-Path $target 'About/About.xml'
$original = [IO.File]::ReadAllBytes($about)
try {
    [xml]$metadata = Get-Content -LiteralPath $about -Raw
    $metadata.ModMetaData.supportedVersions.li = '1.5'
    $metadata.Save($about)
    Expect-Rejection 'untested game version' { & $validator -PackagePath $target }
    [IO.File]::WriteAllBytes($about, $original)
    [xml]$metadata = Get-Content -LiteralPath $about -Raw
    $metadata.ModMetaData.modDependencies.RemoveAll()
    $metadata.Save($about)
    Expect-Rejection 'missing Biotech dependency' { & $validator -PackagePath $target }
    [IO.File]::WriteAllBytes($about, $original)
    [xml]$metadata = Get-Content -LiteralPath $about -Raw
    $metadata.ModMetaData.modVersion = '9.9.9'
    $metadata.Save($about)
    Expect-Rejection 'mismatched assembly version' { & $validator -PackagePath $target }
}
finally { [IO.File]::WriteAllBytes($about, $original) }
$empty = Join-Path $fixture 'empty.dll'
[IO.File]::WriteAllBytes($empty, [byte[]]@())
Expect-Rejection 'empty DLL before cleanup' { Sync-GeneFoundryContent -Source $source -Destination $target -Dll $empty }
Expect-Rejection 'source/destination overlap' { Sync-GeneFoundryContent -Source $source -Destination $source -Dll $dll }
Expect-Rejection 'input DLL inside the cleanup destination' {
    Sync-GeneFoundryContent -Source $source -Destination $target -Dll (Join-Path $target 'Assemblies/GeneFoundry.dll')
}
Expect-Rejection 'filesystem root' { Assert-PlainModDirectory ([IO.Path]::GetPathRoot($repo)) }
& $validator -PackagePath $target
Write-Host 'Release workflow regression checks passed.'
}
finally {
    if (-not $fixture.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path $fixture -Leaf) -notmatch '^GeneFoundry-ReleaseWorkflow-[a-f0-9]{32}$') {
        throw 'Unsafe temporary fixture cleanup.'
    }
    $checked = Assert-PlainModDirectory $fixture
    if (Test-Path -LiteralPath $checked) { Remove-Item -LiteralPath $checked -Recurse -Force }
}
