# GeneFoundry contributor instructions

This is the canonical shared guidance for Codex and GitHub Copilot. Copilot's repository-wide [entry point](.github/copilot-instructions.md) directs it here; keep common rules in this file.

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

- Read the approved policies in [Biomass.md](docs/Biomass.md), [Research.md](docs/Research.md), [Fabrication.md](docs/Fabrication.md) and [ProcessingEnvironment.md](docs/ProcessingEnvironment.md) before changing related behavior. Keep numeric defaults and detailed rules in those documents, rather than copying them here.
- Prefer vanilla systems over custom frameworks. Do not silently change approved balance/storage policies or add settings for them.
- Preserve exactly-once ingredient charging and output, saved progress, ownership and completion receipts. Normal save/load of current jobs and settings is required. Migration of older development saves is not a release requirement, but do not casually rename persisted contracts.

## Verification and documentation

- Follow the commands, coverage limits and runtime checks in [Testing.md](docs/Testing.md). Run portable tests after code/definition changes and managed game regressions for game integration changes when assemblies are available. Add regression tests for corrected behavior, rather than tests that merely mirror implementation.
- Build with `-p:DeployMod=false` during review and CI. Normal production builds deploy; do not overwrite an installed mod as a verification side effect.
- Follow [RELEASE.md](docs/RELEASE.md) for build/package/deployment commands and release checks. Never upload or publish as part of a review.
- Report checks actually run and distinguish managed tests from live RimWorld verification. CI/badge semantics belong in [Pipelines.md](docs/Pipelines.md).

## Documentation ownership

- Keep [README.md](README.md) as a short overview, getting-started pointer and document index. Link to specific documents/anchors instead of copying formulas, command sets, asset specifications or checklists into it.
- Gameplay rules and settings belong in the topic guides linked above. Shared environment rules have one home in `docs/ProcessingEnvironment.md`; other guides link to it.
- Artwork/animation specifications belong in [GeneVatVisuals.md](docs/GeneVatVisuals.md) and [BiomassRefineryOverlay.md](docs/BiomassRefineryOverlay.md). Test commands, coverage and gameplay runtime acceptance belong in `docs/Testing.md`; visual acceptance stays alongside its artwork specifications and is linked from Testing.
- Update the authoritative topic document when behavior changes. Update [CHANGELOG.md](CHANGELOG.md) and the concise Steam BBCode [Workshop listing](docs/workshop-description.txt) where relevant; these summaries may link or describe changes but must not become competing technical specifications.
- Keep dated review findings and executed verification evidence in [Review.md](docs/Review.md), linking to current acceptance instructions rather than duplicating them. Historical evidence is not a claim that later changes were tested.
- When moving documentation, update incoming links and relative paths, preserve unique useful information, and check local targets and heading anchors. Documentation-only edits need link/content checks, not a ceremonial full game build.
