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

function Expect-Rejection([string]$Name, [scriptblock]$Action, [string]$ExpectedMessage) {
    $rejected = $false
    try { & $Action } catch {
        if ($ExpectedMessage -and $_.Exception.Message -notlike "*$ExpectedMessage*") { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw "Expected rejection: $Name" }
    Write-Host "PASS: rejects $Name"
}

# Isolate both source and package so artwork mutations still pass runtime hash checks.
$artRoot = Join-Path $fixture 'Artwork'
$artScripts = Join-Path $artRoot 'scripts'
New-Item -ItemType Directory -Path $artScripts -Force | Out-Null
Copy-Item -LiteralPath $validator, (Join-Path $PSScriptRoot 'ModContent.ps1') -Destination $artScripts
Copy-Item -LiteralPath $source -Destination (Join-Path $artRoot 'Mod') -Recurse
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination (Join-Path $artRoot 'LICENSE')
$artSource = Join-Path $artRoot 'Mod'
$artTarget = Join-Path $artRoot 'Package'
$artValidator = Join-Path $artScripts 'Test-ReleasePackage.ps1'
$transparentIcon = [Drawing.Bitmap]::new(64, 64)
try { $transparentIcon.Save((Join-Path $artSource 'About/ModIcon.png'), [Drawing.Imaging.ImageFormat]::Png) }
finally { $transparentIcon.Dispose() }
Sync-GeneFoundryContent -Source $artSource -Destination $artTarget -Dll $dll
& $artValidator -PackagePath $artTarget
Write-Host 'PASS: accepts transparent mod icon.'
$transparentPreview = [Drawing.Bitmap]::new(640, 360)
try {
    foreach ($root in @($artSource, $artTarget)) {
        $transparentPreview.Save((Join-Path $root 'About/Preview.png'), [Drawing.Imaging.ImageFormat]::Png)
    }
}
finally { $transparentPreview.Dispose() }
Expect-Rejection 'transparent preview' { & $artValidator -PackagePath $artTarget } 'Unexpected transparency: Preview.png'

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
