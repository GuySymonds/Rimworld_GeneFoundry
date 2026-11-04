# GeneFoundry contributor instructions

Deliver the smallest complete, correct change. Inspect existing code before adding abstractions or dependencies. Challenge material scope growth once, then respect explicit decisions. Report only checks actually performed.

## Collaboration

- Default to delivery; explore alternatives when explicitly requested. Recommend the simplest complete approach and stop when the agreed outcome is verified.
- Challenge material scope growth once with its concrete cost, including speculative complexity in your own proposals. Respect deliberate decisions and do not invent follow-up work.
- Be direct and constructive; avoid automatic praise and keep humour occasional and professional.
- The full collaboration guidance remains in [GuySymonds/working-with-me](https://github.com/GuySymonds/working-with-me); this project keeps only its applicable summary, not copied skill files.

## Repository layout and naming

- `Mod/` is the only checked-in runtime content root. Preserve RimWorld's `About`, `Defs`, `Languages`, and `Textures` directory names. Never put tooling or source artwork here.
- `Source/GeneFoundry/GeneFoundry/` contains the .NET Framework 4.7.2 mod. Keep existing feature folders and matching namespaces: `Components`, `Fabrication`, `Jobs`, `PlaceWorkers`, `Recipes`, `Research`, `WorkGivers`. Do not flatten or relocate them without a concrete need.
- `Source/GeneFoundry/GeneFoundry.Tests/` contains .NET 10 xUnit tests. Name test files `<Subject>Tests.cs`; game API tests use the `Game` prefix and `RIMWORLD_TESTS` guard.
- C# filenames use PascalCase; retain RimWorld type prefixes such as `Comp`, `CompProperties_`, `JobDriver_`, `WorkGiver_`, and `Graphic_`. Partial-class files may use a descriptive feature name (`FabricationState.cs`, etc.); do not split every helper into a new file just for naming purity.
- Definition XML filenames describe their subject in PascalCase, inside the appropriate RimWorld definition-type folder. Definitions retain their established `GF_` names. Package ID, persisted type names, Scribe keys, and definition IDs are compatibility contracts, not cleanup targets.
- Use `Biomass`, never `BioMass`, for new paths and names. Runtime animation assets retain the sequence and direction patterns consumed by their loader; do not impose one animation pattern on all machines. Update loaders, fixtures, validators and docs together when moving assets.
- `Source/Textures/` holds editable artwork and source exports; `Mod/Textures/` holds runtime PNGs. Source projects may retain descriptive artist filenames; no automated export is implied.
- `scripts/` holds PowerShell tooling. Executable scripts use Verb-Noun names; `ModContent.ps1` is the shared helper. Dot-source helpers; do not turn them into entry points.
- `docs/` holds technical notes in PascalCase Markdown files; `RELEASE.md` is the release checklist and `workshop-description.txt` is Steam BBCode. Root `README.md`, `CHANGELOG.md`, `LICENSE`, and `AGENTS.md` are conventional exceptions.
- `bin/`, `obj/`, `TestResults/`, and `dist/` are generated, ignored outputs. Never commit game assemblies or Workshop publication IDs. Keep root and runtime LICENSE identical.

## Gameplay decisions

- Raw biomass uses vanilla temperature-based rot: six days at normal temperature, destroyed on spoilage, with a Rare ticker. Refrigeration slows rot and freezing stops it. Biomass hoppers pause rot without power or room cooling, retain prior progress and prevent environmental deterioration. Existing exposure deterioration remains separate outside protected storage.
- Refined biomass is shelf-stable: no rot component and zero outdoor deterioration. It remains damageable. Refinery outage spoilage applies only to incomplete reactions. Completed output waiting for space survives outages/save-load, retries placement without power, uses idle power and receives no processor assistance.
- Refinery preparation must interrupt and stop XP when workstation usability, bill eligibility or worker drafting prevents work. Consume batch ingredients only after successful preparation and final readiness checks.
- Prefer vanilla systems over a custom degradation framework. Do not silently change these policies or add settings for them.

## Verification and documentation

- Run the portable test project after code/definition changes. For managed game tests pass `RimWorldManagedDir` explicitly; the production project's local fallback does not enable game tests.
- Build with `-p:DeployMod=false` during review and CI. Normal production builds deploy; do not overwrite an installed mod as a verification side effect.
- Use `scripts/Release.ps1` and `scripts/Test-ReleaseWorkflow.ps1` for package verification when installed game assemblies are available. Never upload or publish as part of review.
- Update player-facing README, affected technical docs, Workshop text and CHANGELOG when behavior changes. Keep pending work and evidence in `docs/Review.md`; do not describe runtime checks as passed without running the game.
- CI badges describe their actual scope. Portable tests do not certify a production build, package, release, game compatibility, coverage percentage or Unity behavior. See `docs/Pipelines.md`.
