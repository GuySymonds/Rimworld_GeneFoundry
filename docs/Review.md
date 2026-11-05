# Solution review — 4 November 2026

## PR #13 compatibility review and Simple refinery artwork — 5 November 2026

Reviewed the PR against the released v1.0.0 main tree, with particular attention to settings loading, persisted jobs/progress, graphics initialization/directional meshes and vanilla work-effect callbacks. Compared all 70 released Scribe calls: fields, keys, defaults and options are unchanged; the graphics flag is an additional key defaulting to Detailed. Gameplay definition XML is unchanged except graphics paths/classes. Persisted building/job/settings type names and the package identity are retained. No confirmed save/settings regression requiring a production-code fix was identified.

Closed a verification gap: the earlier settings compatibility test removed the graphics key from a payload written by the new serializer, so it could miss a coordinated reader/writer key rename. Added a fixed v1.0.0-format Scribe payload independent of the current writer, covering every released setting with custom values, including 36-hour/18-hour durations. The loader and validation retain those values and enable Detailed when the new key is absent. This is a headless settings-payload check, not a complete released colony save upgrade.

Added the four supplied `BiomassRefinery_Simple_<direction>.png` files unchanged. Hash checks confirm the committed PNG bytes equal the supplied files. New asset regressions require all authored Simple Vat/refinery orientations, PNG format, RGBA and expected directional canvas dimensions. The existing per-direction style loader automatically selects the refinery images; no new renderer, gameplay change or save migration is needed. Editable XCF artwork and unrelated Simple Hopper exports were left untouched.

Verified 375 portable tests and 518 managed tests against the installed game assemblies. A clean export of commit `ea345ae` excluded unrelated artwork added concurrently in the shared working tree: production Release build passed with deployment disabled and zero warnings/errors; package validation passed for 191 runtime files (165 PNGs) and all release workflow safety regressions passed. Documentation links/anchors and `git diff --check` passed. Live rendering, settings UI/restart behavior, a full released-colony save/load upgrade and checks on both declared game versions remain manual work under [runtime acceptance](Testing.md#artwork). No installed-mod deployment, publication or merge into main was performed.

## Graphics styles and runtime texture naming — 5 November 2026

Added the default-enabled, restart-required Enable AI Slop setting using the existing settings infrastructure. Available Simple artwork replaces only its machine/direction; missing optional directions quietly retain Detailed materials, animation and overlays. Simple directions suppress machine animations, bubbles and native recipe visual work effects while retaining work sounds, ordinary corpse/ingredient drawing and actual rot-stink gameplay. Existing draw sizes and positioning remain unchanged, including the supplied square Simple Vat images. A shared Graphic_StyledMachine and Graphic_BiomassRefinery extend native Graphic_Multi; no external dependency or positioning configuration was introduced.

Renamed 156 runtime PNGs using the [style and naming reference](GeneVatVisuals.md#graphics-styles-and-texture-names). Compared Git blob hashes for all 161 PNGs against pre-change branch HEAD: the complete multiset is identical, including transparency and Simple artwork. Compared every definition XML tree with that baseline after excluding only graphic paths/classes: no gameplay definition changes. Source artwork, definition/package IDs, persisted types and existing Scribe keys are retained. External retexture-path compatibility was explicitly excluded by the user.

Verified 373 portable tests and 515 managed tests against the installed game assemblies. New managed regressions cover all sixteen direction-availability combinations for each machine in both settings, native material/preview selection, Simple Vat/refinery presentation guards, ordinary corpse drawing versus Detailed fallback, native visual work-effect suppression, both saved graphics values and all released settings with the new key absent. Existing job, output, ownership and progress Scribe regressions passed. Headless tests substitute Unity materials and do not render artwork. Production Release build passed with deployment disabled and zero warnings/errors; release packaging validated 187 runtime files and all release workflow safety regressions passed. Testhost connectivity required execution outside the sandbox.

Live Unity rendering, a complete released-colony save/load upgrade, settings UI/tooltip layout and compatibility on both game versions remain manual acceptance work in [Testing](Testing.md#artwork). No installed-mod deployment, Steam publication or merge into main was performed.

## v1.1.0 development metadata and deployment — 5 November 2026

Debug and Release now deploy to the same local GeneFoundry directory; version metadata identifies updates. Mod and assembly versions are 1.1.0, with RimWorld 1.6 and 1.7 declared supported. Updated listing, release guidance, metadata checks and deployment regressions accordingly.

Verified 370 portable tests and 505 managed game tests against the installed Managed assemblies, a production build with deployment disabled, release package validation and the complete release workflow safety regressions (including the shared Debug/Release deployment target). Sandboxed testhost connections timed out; the successful test runs used execution outside the sandbox. No installed-mod deployment, Workshop upload or live RimWorld verification was performed. Declaring 1.7 support does not constitute a live compatibility check; follow [runtime acceptance](Testing.md#runtime-acceptance) on both supported versions before publishing.

## v1.0.0 release documentation — 4 November 2026

Consolidated the former Unreleased notes and initial 1.0.0 draft into one dated first-public-release entry, replacing obsolete refinery quantities and timings with the current defaults. README, release guidance and Workshop listing now identify v1.0.0. About.xml and assembly versions already matched 1.0.0 and required no edits.

Verified local link targets in README and the release guide, the new changelog heading anchors, and `git diff --check`. The portable test project compiled, but both the initial run and a no-build/no-restore retry aborted because VSTest could not connect to its test host within 90 seconds; no tests were reported as executed. No gameplay code or definitions changed. No installed-mod deployment, upload, publication or live RimWorld verification was performed.

## Fifteen-processor default — 4 November 2026

Changed the constructor, missing-setting fallback, reset action and UI explanations to a 60-minute automated refinement minimum. The 360-minute base and 20-minute processor reduction are unchanged: 14 processors take 80 minutes, 15 take 60, and further processors provide no benefit. Saved explicit custom minima and legacy percentage conversion are preserved. Updated recipe/building/About descriptions, biomass guidance, first-pack maximum savings and Workshop notes.

Verified 370 portable and 505 managed tests passed, including the new allocation limit and saved old/custom minimum round-trips. Release and Debug builds passed with zero warnings/errors; package validation and release workflow safety regressions passed. Debug deployed to `Mods/GeneFoundry_dev`, with installed DLL, About.xml and refinery recipe/building hashes verified. Saved user settings were read back as 360/60/20 and were not rewritten. No live RimWorld verification was performed; see [refinery acceptance](Testing.md#refinery).

## Release texture optimisation — 4 November 2026

Ran `scripts/Release.ps1` twice with deployment disabled: both builds and package validation passed (183 runtime files, 20 definition files). All 157 runtime texture PNGs were reduced from 54,073,463 to 22,264,757 bytes (58.82% smaller), with byte-identical texture hashes across regenerations. Hash snapshots confirmed `Mod/` and `Source/Textures/` stayed unchanged. No release image-processing failures occurred. The first workflow attempt encountered a sandbox temporary-directory imaging access error; disposable workflow fixtures now live under `dist/`, and the complete rerun passed.

`scripts/Test-ReleaseWorkflow.ps1` passed, including source preservation, repeatable PNG bytes, odd/minimum dimensions, transparent/partial/opaque alpha fixtures, full-resolution Debug deployment hashes, corrupt/full-size/missing texture rejection and existing deployment/metadata/allowlist safety checks. Package validation checked half-size RGBA PNGs and matching vat, Fabricator, refinery preparation and refinery overlay canvases. `git diff --check` passed. No live RimWorld visual quality or animation alignment verification, installed-mod deployment, upload or publication was performed. Use the [release workflow](RELEASE.md#build-and-package) and [artwork acceptance](Testing.md#artwork) for regeneration and live checks.

## Biomass balance dev deployment — 4 November 2026

At the explicit user request, created `codex/dry-mass-biomass-balance` from current main and built Debug with deployment enabled. Build succeeded with zero warnings/errors and deployed to `Mods/GeneFoundry_dev`, preserving the Workshop ID. Verified installed DLL, About metadata/description, material recipe, refinery definition and research definition hashes against the local build/source. No live RimWorld verification was performed. Existing saved refinement settings remain preserved; reset refinement/environment defaults when testing the new 360/120-minute, 20-minutes-per-processor defaults.

## Dry-mass biomass balance and processor timing — 4 November 2026

Implemented meat/plant dry-mass yields (25%/24%, with 60% basic or 75% enzymatic recovery) and decimal whole-corpse yields using recoverable meat × 25% × 2.2 before efficiency/decay. Fixed 50-food batches yield 7 basic or 9 enzymatic biomass. The refinery consumes 45 raw biomass plus 1 neutroamine for exactly 10 refined biomass on success. Following the subsequent balance correction, automated defaults are 360 minutes, reduced by 20 minutes per processor to a 120-minute minimum at 12 processors. Saved duration settings remain; saved percentage assistance converts to equivalent minutes. The legacy XML 2,500-tick migration denominator is unchanged.

Vat timings already meet the requested limits and were retained: material batches take 12 in-game minutes at normal speed; fresh elephant extraction takes about 106.5 minutes at reference skills, excluding hauling and recovery. Refinery preparation remains 30–90 minutes, so its slow end exceeds one hour. Existing corpse meat/condition sampling, wastage, salvage and butcher-product filtering are unchanged; whole corpses still produce no separate meat/leather. Fabrication sources and calculations are unchanged. Inspected the installed Biotech Skin_Green definition and its zero-complexity parent; the existing single-gene price remains 10 refined biomass. Added explicitly qualified first-pack examples to About.xml and the [biomass guide](Biomass.md#green-skin-first-pack-example). Research categories now allow eight rows per column, and the tab label is Gene Foundry; research gates and IDs are unchanged.

Verified: 369 portable tests and 501 managed tests passed, including specified fresh yields, decay, reduced remaining-material inputs, wastage, ingredient metadata, hopper consumption at 44/45/50 available biomass, the twelve-processor timing/allocation boundary, saved settings conversion/round-trips and existing refinery lifecycle/fabrication regressions. Release build succeeded with zero warnings/errors using deployment disabled; local package validation passed (183 runtime files, 20 definition files); release workflow safety regressions passed using disposable fixtures. No installed mod deployment, push, publication or live RimWorld verification was performed. Managed fixtures do not simulate live missing-limb stat recalculation, actual colony hauling, complete corpse salvage, or UI rendering; use the [extraction](Testing.md#biomass-storage-and-extraction), [refinery](Testing.md#refinery) and [research](Testing.md#research) acceptance checks.


## Development deployment verification — 4 November 2026

Debug builds now deploy to `Mods/GeneFoundry_dev`; Release builds deploy to `Mods/GeneFoundry`. Missing or empty targets are supported, while nonempty targets require matching package metadata. The local installation was renamed from `GeneticBiomassFabrication` to `GeneFoundry` and the user deployment variable updated. The naming audit found only intentional legacy package-ID references; that compatibility identity remains unchanged.

Verified after this change: 349 portable tests passed; release staging and package validation passed (183 runtime files, 20 definition files); release workflow regressions passed, including new missing/empty deployment, configuration isolation and invalid-target cases; an actual Debug build deployed successfully to `GeneFoundry_dev` with zero warnings/errors. Release target selection was exercised in a disposable fixture. No live RimWorld or Workshop verification was performed. See [release instructions](RELEASE.md#development-deployment) for configuration and [Testing](Testing.md#runtime-acceptance) for live acceptance.

Reviewed the extraction, medical recovery, refinement, processor allocation, fabrication/bill ownership, research, settings, animation, XML definitions, tests, packaging/deployment scripts, documentation and tracked folder/file conventions. GitHub reported no open issues and no workflow runs for `main` at review time. This is a code and managed-API review; it does not certify live-colony behavior or every third-party mod combination.

## Resolved refinery findings

| Priority | Original finding | Resolution |
|---|---|---|
| P2 | Preparation kept waiting/granting XP after power loss or bill suspension/deletion. | Fixed: a global job failure condition checks workstation/bill eligibility and drafting; the learning callback independently checks eligibility. Managed tests rebuild toils after a Scribe round-trip and exercise power loss, native bill deletion, suspension, missing bill, drafting and despawn without further XP or batch start. |
| P2 | Completed output waiting for space could spoil during a power outage. | Fixed: completed reactions retry placement before power/outage handling. Existing saved progress and completion receipts preserve success without new save fields. Waiting output uses idle power, accepts no processors, survives outages/save-load and places exactly once without rerolling. An incomplete-reaction test retains the 5,000-tick spoilage boundary. |

Both code findings are resolved and covered by 10 managed lifecycle regression cases in `GameRefineryLifecycleTests.cs`. Output-placement and notification boundaries are replaced by headless fixtures; preparation tests execute the real registered job failure and learning callbacks and vanilla workstation power usability. They do not simulate a live colony or Unity. Live-game acceptance is tracked under required release work below.

## Required release work

- Complete [runtime acceptance](Testing.md#runtime-acceptance) on the actual packaged mod and the [human release checklist](RELEASE.md#human-release-checklist). These are the authoritative checklists; they have not been performed by the automated review.
- Push the workflow and inspect its first hosted run, then configure the required check described in [Pipelines.md](Pipelines.md). No GitHub settings, commits, pushes or uploads were performed by this review.

## Corrections delivered

- Raw biomass now has a Rare-ticking vanilla-derived rot component: six days at normal temperature, destroyed on spoilage. Sealed biomass hoppers pause spoilage without power, retain age and prevent environmental deterioration. Refined biomass remains non-rotting, zero-deterioration and damageable.
- Fixed missing refinery-preparation source inclusion that prevented the optional managed API suite from compiling.
- Fixed release validation treating the custom directional Fabricator graphic as a single PNG. Also explicitly validate refinery and fabricator animation assets referenced from code.
- Corrected documentation claiming `/p:PostBuildEvent=` disabled deployment; the actual switch is `-p:DeployMod=false`. Corrected the developer-command zero-risk claim and Workshop environmental-risk wording. Documented refinery preparation frames.
- Standardized `BioMassRefineryFramesAndOverlays` to `BiomassRefineryFrames`, source `BioMassHopper.png` to `RawBiomassHopper.png`, and `deploy.ps1` to `Deploy-Mod.ps1`, updating consumers. Existing machine-specific animation patterns and editable artist filenames remain intentional conventions, rather than forcing a mass rename of working assets.
- Added root [AGENTS.md](../AGENTS.md) with structure, naming, compatibility, gameplay and verification decisions. Retained the existing feature folders and project nesting: restructuring them would change many paths without fixing a functional problem.
- Added portable CI and scoped badges. See [Pipelines.md](Pipelines.md) for actual coverage and what full game/package automation would require.

## Verification evidence

- Production Release build: passed, zero warnings and errors, deployment disabled.
- Managed game-API-enabled suite after refinery fixes: 466 passed, zero failures/skips. Includes the 10 refinery lifecycle cases above and a real managed ThingGrid fixture verifying hopper-only preservation, both rot tick entry points, unchanged accumulated age and removal/despawn behavior; it does not start Unity.
- Portable suite: 349 passed, zero failures/skips, using the exact CI command with Release configuration, TRX output and hang detection.
- Release package: validated 183 runtime files and 20 definition files, metadata, assembly versions, artwork and runtime assets.
- Release workflow regression: passed publication-ID preservation, stale-file cleanup, sibling safety, invalid assets/metadata and unsafe-target rejection using a disposable fixture.
- All PowerShell scripts parse; `git diff --check` passes. Testhost localhost connections and the disposable release fixture required execution outside the filesystem/network sandbox.
- Hosted Actions and live RimWorld checks have not run. No installed mod was deployed or Workshop item published.
