# Deterministic safety/regression checks against a disposable copy, never an installed mod.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ModContent.ps1')
. (Join-Path $PSScriptRoot 'Resize-ReleaseTextures.ps1')
$repo = Split-Path $PSScriptRoot -Parent
$tempRoot = Assert-PlainModDirectory (Join-Path $repo 'dist')
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
Resize-ReleaseTextures -PackagePath $target
& $validator -PackagePath $target

# Recopy originals before each optimisation: repeated packaging must be byte-identical.
function Get-TextureHashes([string]$Root) {
    return @(Get-ChildItem -LiteralPath (Join-Path $Root 'Textures') -Recurse -File -Filter '*.png' |
        Sort-Object FullName | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName).Hash }) -join ','
}
$sourceHashes = Get-TextureHashes $source
$firstHashes = Get-TextureHashes $target
Sync-GeneFoundryContent -Source $source -Destination $target -Dll $dll
Resize-ReleaseTextures -PackagePath $target
if ((Get-TextureHashes $target) -ne $firstHashes -or (Get-TextureHashes $source) -ne $sourceHashes) {
    throw 'Texture optimisation is not repeatable or modified source textures.'
}
Write-Host 'PASS: repeatable texture bytes and unchanged source textures.'

# Odd dimensions, single-pixel axes and alpha values.
$imageFixture = Join-Path $fixture 'Images'
$imageTextures = Join-Path $imageFixture 'Textures'
New-Item -ItemType Directory -Path $imageTextures -Force | Out-Null
foreach ($alpha in @(0, 128, 255)) {
    $image = [Drawing.Bitmap]::new(5, 3, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($image)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb($alpha, 80, 120, 160))
        $image.Save((Join-Path $imageTextures "$alpha.png"), [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $image.Dispose() }
}
$tiny = [Drawing.Bitmap]::new(1, 1)
try { $tiny.Save((Join-Path $imageTextures 'tiny.png'), [Drawing.Imaging.ImageFormat]::Png) }
finally { $tiny.Dispose() }
Resize-ReleaseTextures -PackagePath $imageFixture
foreach ($alpha in @(0, 128, 255)) {
    $image = [Drawing.Bitmap]::FromFile((Join-Path $imageTextures "$alpha.png"))
    try {
        if ($image.Width -ne 3 -or $image.Height -ne 2) { throw 'Incorrect odd-dimension rounding.' }
        foreach ($x in 0..2) { foreach ($y in 0..1) {
            if ($image.GetPixel($x, $y).A -ne $alpha) { throw 'Alpha was not preserved.' }
        } }
    }
    finally { $image.Dispose() }
}
$image = [Drawing.Image]::FromFile((Join-Path $imageTextures 'tiny.png'))
try { if ($image.Width -ne 1 -or $image.Height -ne 1) { throw 'Invalid minimum dimensions.' } }
finally { $image.Dispose() }
Write-Host 'PASS: odd dimensions, minimum dimensions and transparent/partial/opaque alpha.'

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
Resize-ReleaseTextures -PackagePath $artTarget
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
Resize-ReleaseTextures -PackagePath $target
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
    $deploy = Join-Path $PSScriptRoot 'Deploy-Mod.ps1'
    & $deploy -Dll $dll -Configuration Debug
    if ((Get-TextureHashes $env:GENEFOUNDRY_MOD_DIR) -ne $sourceHashes) { throw 'Debug deployment changed texture content.' }
    if (-not (Test-Path -LiteralPath (Join-Path $env:GENEFOUNDRY_MOD_DIR 'Assemblies/GeneFoundry.dll')) -or
        (Test-Path -LiteralPath ($env:GENEFOUNDRY_MOD_DIR + '_dev'))) { throw 'Debug deployment must use the shared mod folder.' }
    $staleMarker = Join-Path $env:GENEFOUNDRY_MOD_DIR 'debug-only.txt'
    Set-Content -LiteralPath $staleMarker -Value 'Remove during Release deployment'
    & $deploy -Dll $dll -Configuration Release
    if (-not (Test-Path -LiteralPath (Join-Path $env:GENEFOUNDRY_MOD_DIR 'Assemblies/GeneFoundry.dll')) -or
        (Test-Path -LiteralPath $staleMarker)) { throw 'Release deployment did not refresh the shared folder.' }
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
    Write-Host 'PASS: shared Debug/Release target, stale-file removal and deployment guards.'
}
finally { $env:GENEFOUNDRY_MOD_DIR = $previousModDir }

$stray = Join-Path $target 'Assemblies/GeneFoundry.pdb'
Set-Content -LiteralPath $stray -Value 'debug symbols'
Expect-Rejection 'debug symbols' { & $validator -PackagePath $target }
Remove-Item -LiteralPath $stray
$texture = Join-Path $target 'Textures/GeneFoundry/Buildings/GeneVat_Detailed_north.png'
$textureBackup = Join-Path $fixture 'texture-backup.png'
Copy-Item -LiteralPath $texture -Destination $textureBackup
try {
    Copy-Item -LiteralPath (Join-Path $source 'Textures/GeneFoundry/Buildings/GeneVat_Detailed_north.png') -Destination $texture -Force
    Expect-Rejection 'full-resolution release texture' { & $validator -PackagePath $target } 'Wrong release texture dimensions'
    [IO.File]::WriteAllText($texture, 'invalid PNG')
    Expect-Rejection 'corrupt release texture' { & $validator -PackagePath $target }
}
finally { Move-Item -LiteralPath $textureBackup -Destination $texture -Force }
Move-Item -LiteralPath $texture -Destination $textureBackup
try { Expect-Rejection 'missing runtime texture' { & $validator -PackagePath $target } }
finally { Move-Item -LiteralPath $textureBackup -Destination $texture }
$about = Join-Path $target 'About/About.xml'
$original = [IO.File]::ReadAllBytes($about)
try {
    [xml]$metadata = Get-Content -LiteralPath $about -Raw
    $metadata.ModMetaData.supportedVersions.SelectSingleNode('li').InnerText = '1.5'
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
