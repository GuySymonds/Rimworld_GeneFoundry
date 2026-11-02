# Deterministic safety/regression checks against a disposable copy, never an installed mod.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$fixture = Assert-PlainModDirectory (Join-Path $repo '.artifacts/ReleaseWorkflow')
$target = Join-Path $fixture 'GeneFoundry'
$dll = Join-Path $repo 'dist/GeneFoundry/Assemblies/GeneFoundry.dll'
$validator = Join-Path $PSScriptRoot 'Test-ReleasePackage.ps1'
if (-not (Test-Path -LiteralPath $dll)) { throw 'Run scripts/Release.ps1 first.' }
if (Test-Path -LiteralPath $fixture) {
    if ($fixture -ne [IO.Path]::GetFullPath((Join-Path $repo '.artifacts/ReleaseWorkflow'))) { throw 'Unsafe fixture cleanup.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
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
Sync-GeneFoundryContent -Source $source -Destination $target -Dll $dll
if (Test-Path -LiteralPath $stale) { throw 'Stale definitions survived deployment.' }
if ([Convert]::ToBase64String($idBefore) -ne [Convert]::ToBase64String([IO.File]::ReadAllBytes($id))) {
    throw 'Deployment modified the Workshop publication ID.'
}
if ((Get-Content -LiteralPath $sentinel).Trim() -ne 'Do not change') { throw 'Deployment touched a sibling.' }
Write-Host 'PASS: publication ID and sibling preserved; obsolete definition removed.'

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
Write-Host 'Release workflow regression checks passed. Disposable fixtures remain under .artifacts/ReleaseWorkflow.'
