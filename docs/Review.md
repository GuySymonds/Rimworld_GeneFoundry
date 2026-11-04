# Solution review — 4 November 2026

## Six-row research layout — 4 November 2026

Placed Enzymatic extraction directly below Gene fabrication and moved category projects to a nearby grid on the right. Categories fill down six rows before adding another column, without a column limit. Stable IDs, costs and prerequisites are unchanged. Verified 354 portable and 475 managed tests passed; the layout regression covers 43 categories across eight columns, six rows, unique positions and load-order stability. Debug build passed with zero warnings/errors and deployed to `Mods/GeneFoundry_dev`; installed DLL and research XML hashes matched. Displayed spacing still requires [live research acceptance](Testing.md#research).

## Mod icon transparency validation — 4 November 2026

Limited opacity sampling to Preview.png; ModIcon.png may use transparency. Artwork dimensions, PNG format, file-size limits and runtime file hashes remain unchanged. The release guide now explicitly permits icon transparency; the documentation search found no other icon opacity restriction.

Verified the full release workflow regression script passed, including new persistent cases accepting a transparent 64×64 icon and rejecting a transparent preview for the expected opacity error. Disposable full-validator checks also accepted original artwork and rejected wrong dimensions, non-PNG data and 1,000,000-byte files for both images. The initial sandboxed workflow run could not move a temporary texture; the unrestricted rerun passed. No game build, deployment or live RimWorld verification was performed for this tooling-only change.

## Ranked gene pricing follow-up — 4 November 2026

Replaced the pack-wide discount with per-gene biomass charges: base cost descending, then name, at 100%, 90%, 80%, 70%, 60%, and 50% for every later gene. Fractional charges are summed before rounding up once. The picker, availability checks, material commitment and unpaid legacy cancellation fallback use the same pricing function; paid cancellation receipts remain authoritative. Selected cards and each library category use cost/name order. Archite capsules and work are unchanged.

Verified 354 portable and 475 managed tests passed, including unequal-cost distributions, ranked minimums, combined rounding, deterministic ordering and exactly-once material commitment. Debug build passed with zero warnings/errors and deployed to `Mods/GeneFoundry_dev`; installed DLL hash matched. No live RimWorld check was performed; see [fabrication acceptance](Testing.md#fabrication).

## Adaptive gene picker follow-up — 4 November 2026

Selected genes now use only the rows needed (a compact empty state, growing to 38% before scrolling); larger 110 × 90 gene cards share column/row calculations with the library. Read-only details can use the full gene area. Verified 347 portable and 467 managed tests passed; Debug build passed with zero warnings/errors, deployed to `Mods/GeneFoundry_dev`, and installed DLL hash matched the build. Live layout acceptance remains under [fabrication checks](Testing.md#fabrication).

## Research layout follow-up — 4 November 2026

Moved Gene fabrication to the left of Enzymatic extraction on the same row. Verified 347 portable tests passed; deployed to `Mods/GeneFoundry_dev` and verified the installed research XML hash matches the source. Displayed placement still needs live RimWorld verification.

## Research persistence, research tree, gene picker and corpse eligibility — 4 November 2026

Completed Gene fabrication/category project IDs are saved independently of research points and restored after loading. Unfinished projects retain their points against current costs, and new categories remain unresearched. Legacy saves cannot identify formerly completed projects already made partial before completion records existed. Extraction/refinement unlocks now belong to Gene fabrication; Enzymatic extraction follows separately, and generated categories show their foundation prerequisite. The picker starts collapsed, has Show all / Hide all, and moves detailed skill/risk/recovery text into a footer tooltip. Corpse recipes resolve an organic race definition allow list for ingredients and bill choices, preserving herbal catalysts and the shared runtime eligibility rule.

Verified: 347 portable tests and 467 managed tests passed; Release build completed with zero warnings/errors, deployment disabled; staged package validation passed (183 runtime files, 20 definition files); release workflow safety regressions passed during this change. Managed research persistence exercises Scribe with vanilla completion side effects replaced; corpse filter tests exercise actual ThingFilters with headless definitions. Debug build deployed to `Mods/GeneFoundry_dev` using the separate configuration-aware deployment branch tooling; installed and built DLL SHA-256 hashes match. No live RimWorld verification was performed. Follow [research acceptance](Testing.md#research), [fabrication acceptance](Testing.md#fabrication) and [corpse acceptance](Testing.md#biomass-storage-and-extraction) for the displayed tree, picker layout, real modded definitions and existing bill behavior.

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
