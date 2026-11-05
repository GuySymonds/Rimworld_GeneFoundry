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

# The tracked source ID is authoritative; validate before any destination cleanup.
function Assert-WorkshopPublicationIdentity([string]$Source, [string]$Destination) {
    $sourceId = Join-Path $Source 'About/PublishedFileId.txt'
    $targetId = Join-Path $Destination 'About/PublishedFileId.txt'
    if (-not (Test-Path -LiteralPath $sourceId -PathType Leaf)) {
        throw "Missing Workshop publication ID: $sourceId"
    }
    $expected = [IO.File]::ReadAllText($sourceId).Trim()
    if ($expected -notmatch '^[1-9][0-9]*$') { throw "Invalid Workshop publication ID: $sourceId" }
    if (Test-Path -LiteralPath $targetId) {
        $installed = [IO.File]::ReadAllText($targetId).Trim()
        if ($installed -notmatch '^[1-9][0-9]*$') { throw "Invalid Workshop publication ID: $targetId" }
        if ($installed -ne $expected) {
            throw 'Source and destination Workshop publication IDs disagree. Resolve this before copying.'
        }
    }
    return $expected
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
    Assert-WorkshopPublicationIdentity -Source $sourceRoot -Destination $targetRoot | Out-Null
    New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null
    # Fresh deployment: remove every stale local file, except the Workshop identity.
    foreach ($item in Get-ChildItem -LiteralPath $targetRoot -Force) {
        $items = if ($item.Name -eq 'About' -and $item.PSIsContainer) {
            @(Get-ChildItem -LiteralPath $item.FullName -Force | Where-Object { $_.Name -ne 'PublishedFileId.txt' })
        } else { @($item) }
        foreach ($stale in $items) {
            if (-not $stale.FullName.StartsWith($targetRoot + [IO.Path]::DirectorySeparatorChar,
                [StringComparison]::OrdinalIgnoreCase)) { throw 'Cleanup escaped mod directory.' }
            Remove-Item -LiteralPath $stale.FullName -Recurse -Force
        }
    }
    foreach ($folder in @('Defs', 'Textures', 'Patches', 'Languages')) {
        $from = Join-Path $sourceRoot $folder
        if (Test-Path -LiteralPath $from) {
            Copy-Item -LiteralPath $from -Destination (Join-Path $targetRoot $folder) -Recurse -Force
        }
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
