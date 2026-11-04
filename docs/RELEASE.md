# GeneFoundry release and Workshop publishing

## Build and package

Use Windows PowerShell 5.1 or PowerShell 7 on Windows, the .NET 10 SDK (tests), .NET Framework 4.7.2 targeting/reference assemblies, and an installed RimWorld 1.6 with Biotech. No game DLLs are redistributed. The committed legacy S: drive path remains a fallback for the original developer; other installations pass their Managed directory explicitly or set the `RimWorldManagedDir` environment variable.

From the repository root:

```powershell
pwsh -NoProfile -File scripts/Release.ps1 -RimWorldManagedDir 'C:\Path To\RimWorld\RimWorldWin64_Data\Managed'
```

With the original local path or the environment variable, the one-command form is:

```powershell
pwsh -NoProfile -File scripts/Release.ps1
```

This builds Release with `DeployMod=false` and the post-build event disabled, stages `dist/GeneFoundry`, and validates the result. It never copies into the game or Steam directories. Only that fixed staging directory is cleaned. Linked/reparse-point paths are rejected. `About/PublishedFileId.txt` is retained in place; source and destination IDs must agree when both exist. Packaging fails on missing assets, stray development files, inconsistent versions, missing DLC metadata, invalid artwork or unresolved local texture paths. Validation is also available separately:

```powershell
pwsh -NoProfile -File scripts/Test-ReleasePackage.ps1
```

`Mod/` contains the checked-in runtime assets: About, Defs, Languages, Textures and LICENSE. Source artwork stays in `Source/Textures`; it is never packaged. The root LICENSE remains the repository license; keep `Mod/LICENSE` identical when changing it. Patches are copied if used; the current mod has none. The generated package contains the compiled DLL, never C# source, tests, source artwork, symbols, tooling or these documents. No ZIP or public GitHub release is required for Workshop upload.

## Automated verification

Run the [automated test commands](Testing.md#automated-commands), then build and validate the package as described above. After packaging, run the release safety regressions:

```powershell
pwsh -NoProfile -File scripts/Test-ReleaseWorkflow.ps1
```

The regression script checks publication-ID preservation, stale-file removal, target safety and negative package validation using a uniquely named temporary directory that it removes afterward. It never deploys to the game.

Hosted automation and badge scope are documented in [Pipelines.md](Pipelines.md). Complete the [runtime acceptance checks](Testing.md#runtime-acceptance) using the actual package before publishing, and review [open release work and dated evidence](Review.md).

## Build without deployment

For a standalone production build, without packaging or installation:

```powershell
dotnet build Source/GeneFoundry/GeneFoundry/GeneFoundry.csproj -c Release -p:DeployMod=false
```

Pass `-p:RimWorldManagedDir=...` when the environment variable/local fallback does not identify your game installation. Normal builds enable the deployment target; retain `DeployMod=false` during review and CI.

## Version updates

Keep version management explicit:

1. Update `AssemblyVersion` and `AssemblyFileVersion` in `Source/GeneFoundry/GeneFoundry/Properties/AssemblyInfo.cs` to `major.minor.patch.0`, and `AssemblyInformationalVersion` to `major.minor.patch`.
2. Set `Mod/About/About.xml`'s `modVersion` to the same `major.minor.patch`.
3. Update CHANGELOG and Workshop change notes. Keep `supportedVersions` at `1.6` unless another game version has actually been tested.
4. Run the tests and packaging command. The validator compares the compiled assembly/file/informational versions with About.xml. Startup logging derives its version from the assembly; there is no separate log version to update.

Do not change the established `GBF.GeneticBiomassFabrication` package ID when updating the display name or release version. This identity came from the existing local mod metadata.

## Artwork and listing

- Review `Mod/About/Preview.png` (640 x 360, opaque PNG, under 1 MB) and `Mod/About/ModIcon.png` (64 x 64).
- Paste `docs/workshop-description.txt` into the Workshop description editor. It uses Steam BBCode, not GitHub Markdown.
- Initial release notes are in `CHANGELOG.md`.
- To regenerate only the Workshop artwork from existing sprites: `pwsh -NoProfile -File scripts/New-WorkshopArtwork.ps1`. This uses built-in Windows System.Drawing and does not modify runtime sprites. Regeneration is not required to build a release.

## Human release checklist

- [ ] Approve the preview, icon, description and release notes.
- [ ] Copy `dist/GeneFoundry` to a local `RimWorld/Mods/GeneFoundry` folder. Disable any older development copy with the same package ID and any Workshop copy; do not activate duplicates.
- [ ] Load the actual package with Core, Biotech and GeneFoundry only. Confirm the intended version and no startup/definition errors, then complete [gameplay and artwork runtime acceptance](Testing.md#runtime-acceptance).
- [ ] With Steam running and the owning account signed in, enable Development mode, open the vanilla Mods menu, select the local mod and use its Advanced/Upload to Steam control. Review the upload dialog, title, description, preview and change notes. Follow any Steam Workshop legal-agreement prompt. Do not use a subscribed copy as the upload source.
- [ ] Check the Workshop item's supported game version and Biotech requirement, verify the uploaded preview/listing, and choose the intended visibility. Upload privately first if desired; make it public only after approval.
- [ ] Preserve the resulting local `About/PublishedFileId.txt`: copy it to `Mod/About/PublishedFileId.txt` (Git-ignored) and keep a private backup. It identifies the existing Workshop item and must accompany future updates.

For later updates, retain the same publication ID and package ID, rebuild/validate, copy or deploy the new content into the same local upload folder, and use the same upload control with appropriate change notes. Never delete the ID to publish an update: that creates a separate Workshop item. The release and deployment scripts preserve the destination ID and reject conflicting source/destination IDs.

## Development deployment

Source/Textures is the working artwork directory; export finished PNGs manually into Mod/Textures. Builds leave final textures untouched and deploy from Mod/ after every Build/Rebuild, even when compilation is already up to date. Source artwork and XCF projects are never copied. Set `GENEFOUNDRY_MOD_DIR` to an existing local mod directory whose About.xml has the established package ID. `scripts/Deploy-Mod.ps1` now reads runtime assets exclusively from `Mod/`, deletes stale local contents and copies About/artwork, LICENSE and all runtime folders fresh, preserving only About/PublishedFileId.txt. It can also be invoked explicitly:

```powershell
$env:GENEFOUNDRY_MOD_DIR = 'C:\Path To\RimWorld\Mods\GeneFoundry'
pwsh -NoProfile -File scripts/Deploy-Mod.ps1 -Dll ./dist/GeneFoundry/Assemblies/GeneFoundry.dll
```

Use `-p:DeployMod=false` for standalone builds. The release script always disables deployment regardless of environment settings. Close RimWorld before replacing a DLL.

Publishing instructions were cross-checked against the developer's Workshop guidance on the [Ludeon forum](https://ludeon.com/forums/index.php?topic=33077.0); metadata field names were verified against the installed RimWorld 1.6 assembly and Biotech About.xml. UI wording may vary by game language.
