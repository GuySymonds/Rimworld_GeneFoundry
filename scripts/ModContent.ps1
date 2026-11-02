# Shared by development deployment and release staging. Never deletes publication IDs.
function Assert-PlainModDirectory([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ($full -eq [IO.Path]::GetPathRoot($full).TrimEnd([IO.Path]::DirectorySeparatorChar)) {
        throw 'A filesystem root cannot be a mod directory.'
    }
    for ($ancestor = $full; $ancestor; $ancestor = Split-Path $ancestor -Parent) {
        if (Test-Path -LiteralPath $ancestor) {
            $item = Get-Item -LiteralPath $ancestor -Force
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Refusing linked directory: $ancestor"
            }
        }
    }
    if (Test-Path -LiteralPath $full) {
        foreach ($item in Get-ChildItem -LiteralPath $full -Force -Recurse) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Refusing linked content: $($item.FullName)"
            }
        }
    }
    return $full
}

function Sync-GeneFoundryContent([string]$Source, [string]$Destination, [string]$Dll) {
    $sourceRoot = Assert-PlainModDirectory $Source
    $targetRoot = Assert-PlainModDirectory $Destination
    if ($sourceRoot -eq $targetRoot -or $sourceRoot.StartsWith($targetRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase) -or $targetRoot.StartsWith($sourceRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Mod source and destination must be separate, non-overlapping directories.'
    }
    foreach ($required in @('About/About.xml', 'About/Preview.png', 'LICENSE')) {
        if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot $required) -PathType Leaf)) {
            throw "Missing runtime asset: $required"
        }
    }
    foreach ($required in @('Defs', 'Languages', 'Textures')) {
        if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot $required) -PathType Container)) {
            throw "Missing runtime folder: $required"
        }
    }
    if (-not (Test-Path -LiteralPath $Dll -PathType Leaf) -or (Get-Item -LiteralPath $Dll).Length -eq 0) {
        throw "Compiled DLL not found or empty: $Dll"
    }
    if ([IO.Path]::GetFullPath($Dll).StartsWith($targetRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The input DLL cannot be inside the destination being refreshed.'
    }
    $sourceId = Join-Path $sourceRoot 'About/PublishedFileId.txt'
    $targetId = Join-Path $targetRoot 'About/PublishedFileId.txt'
    foreach ($id in @($sourceId, $targetId)) {
        if ((Test-Path -LiteralPath $id) -and (Get-Content -LiteralPath $id -Raw).Trim() -notmatch '^[1-9][0-9]*$') {
            throw "Invalid Workshop publication ID: $id"
        }
    }
    if ((Test-Path -LiteralPath $sourceId) -and (Test-Path -LiteralPath $targetId) -and
        (Get-Content -LiteralPath $sourceId -Raw).Trim() -ne (Get-Content -LiteralPath $targetId -Raw).Trim()) {
        throw 'Source and destination Workshop publication IDs disagree. Resolve this before copying.'
    }
    New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null
    foreach ($folder in @('Assemblies', 'Defs', 'Textures', 'Patches', 'Languages')) {
        $target = [IO.Path]::GetFullPath((Join-Path $targetRoot $folder))
        if (-not $target.StartsWith($targetRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Cleanup escaped mod directory: $target"
        }
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
        $from = Join-Path $sourceRoot $folder
        if (Test-Path -LiteralPath $from) { Copy-Item -LiteralPath $from -Destination $target -Recurse -Force }
    }
    $targetAbout = Join-Path $targetRoot 'About'
    New-Item -ItemType Directory -Path $targetAbout -Force | Out-Null
    foreach ($file in @('About.xml', 'Preview.png', 'ModIcon.png', 'PublishedFileId.txt')) {
        $from = Join-Path $sourceRoot "About/$file"
        if (Test-Path -LiteralPath $from) { Copy-Item -LiteralPath $from -Destination (Join-Path $targetAbout $file) -Force }
    }
    $assemblies = Join-Path $targetRoot 'Assemblies'
    New-Item -ItemType Directory -Path $assemblies -Force | Out-Null
    Copy-Item -LiteralPath $Dll -Destination (Join-Path $assemblies 'GeneFoundry.dll') -Force
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'LICENSE') -Destination (Join-Path $targetRoot 'LICENSE') -Force
}
