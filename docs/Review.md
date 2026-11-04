# Solution review — 4 November 2026

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
