# Testing and runtime acceptance

[Back to overview](../README.md) · [Build and package](RELEASE.md#build-and-package) · [Pipeline scope](Pipelines.md)

## Automated commands

Run from the repository root with the .NET 10 SDK. The portable suite does not require a game installation:

```powershell
dotnet test Source/GeneFoundry/GeneFoundry.Tests/GeneFoundry.Tests.csproj -c Release -p:RimWorldManagedDir=
```

To also compile and run managed game API regressions, pass an installed RimWorld 1.6 Managed directory explicitly:

```powershell
dotnet test Source/GeneFoundry/GeneFoundry.Tests/GeneFoundry.Tests.csproj -c Release '-p:RimWorldManagedDir=C:\Path To\RimWorld\RimWorldWin64_Data\Managed'
```

The production project's local assembly-path fallback does not enable conditional game tests. The test project links production sources and does not deploy the mod. For the complete production build, package validation and release safety regressions, follow [RELEASE.md](RELEASE.md#build-and-package). Hosted automation and badge semantics belong in [Pipelines.md](Pipelines.md).

## Coverage and limits

The .NET 10 xUnit project compiles the production `BiomassCalculations.cs` source directly, without loading RimWorld or running the deployment script. This tests the exact math used by the mod while keeping the test runner independent of a game installation. It covers yield and rounding, damaged bodies, minimum recovery, decay/handling timing, equal-yield comparisons, batch efficiency, operator speed and configurable minutes.

The same test project also checks all mod definition XML for parsing and surrounding text whitespace, duplicate definition names, unresolved GeneFoundry references, recipe/product reference types and custom classes included in the production project. Regression checks cover Gene Vat recipe/workgiver registration and corpse ingredient safety/decay settings. Managed corpse-filter regressions verify an organic definition allow list excludes unknown mechanical corpses while retaining the mandatory herbal catalyst. Fixtures are copied from the real definitions and sources at build time; no game installation is needed for these checks.

The game mod remains .NET Framework 4.7.2. These static checks do not replace RimWorld's definition loading, inheritance and cross-references to vanilla or other mods. UI rendering, job behavior and game-object integration still require in-game testing.

The managed API tests use isolated managed game objects without launching Unity. They check the actual public `HaulAIUtility.UpdateJobWithPlacedThings` callback, merged and separate ingredient receipts, missing-receipt completion rejection, and all four rotations of the corpse processing cell. Without an explicit `RimWorldManagedDir`, the portable calculation/XML suite remains independent of the game installation.

### Refinery lifecycle

Managed refinery lifecycle regressions exercise real job failure/learning callbacks and Scribe round-trips: preparation duration/remaining ticks survive loading, then power loss, bill suspension/deletion, drafting and workstation despawn stop eligibility and XP without starting a batch. Completed-output checks cover the first success transition, long outages, save/load, no reroll, idle power/no assistance and exactly-once placement. Incomplete reactions still spoil at precisely 5,000 outage ticks. Headless fixtures replace output placement and player notifications; live-colony scheduling and Unity rendering remain manual checks.

### Fabrication state

The portable suite covers deterministic costs, skills, risk and work-weighting, plus actual definition/source registration. The optional installed-game suite additionally exercises real unfinished state, partial delivery accounting, exact discounted commitment (retaining excess delivered materials), repeated commitment, pawn ownership, overrun clamping, invalid developer state, cancellation, inactive work rejection, success/failure and vanilla multi-gene initialization. Scribe tests round-trip the job fields and actual owner reference through saving, loading, cross-reference resolution and post-load initialization, plus bill genes and completed state. Bill lifecycle tests cover ordering, suspension, exactly-once removal on success/failure and deleting an active bill. Headless fixtures replace Unity object creation, destruction notifications and map placement; they do not simulate a running colony or a full save-load cycle.

## Runtime acceptance

Use a disposable colony and the actual packaged mod. Automated managed tests do not start Unity, render artwork or exercise full-colony scheduling. Reset temporary fast settings after checking.

### Biomass storage and extraction

- Compare warm storage, refrigeration, freezing and hopper preservation using [the storage policy](Biomass.md#storage-and-degradation). Move partially aged stacks in/out, split/merge, haul them, destroy a hopper and save/reload. Confirm prior age is retained and refined biomass stays shelf-stable.
- Compare total meat, plant and herb quantities before/after completed batches, confirm catalyst consumption from multiple stacks, and verify corpse positioning and interruption in-game.
- With modded organic races and robots loaded, verify both corpse bill filters offer organic corpses, exclude mechanical corpses, retain animal-only defaults and permit the enzymatic herbal catalyst. Existing bill selections must remain constrained by the recipe allow list.
- Process a corpse with multiple implants; compare low/high qualified medics; confirm no medicine is consumed; compare dirty/clean rooms with identical lighting; interrupt after one removal and verify it does not reappear or produce twice; confirm extraction continues after successful and failed salvage. Mods that supply custom removal effects rather than a standard removal item may need compatibility support.

### Refinery

Use a disposable save and short processing settings:

1. Run two refineries near shared processors; confirm no processor assists both and excess capacity can assist the second refinery.
2. Start, interrupt and cancel an assembler job. Verify it retains priority during operator breaks and releases processors when its job ends.
3. Save/reload during preparation, assisted processing and a power outage. Check progress, learning, risk and outage time continue correctly. Cut power, suspend/delete a bill or draft its worker during preparation: preparation and XP must stop without charging ingredients. Block completed output, save/reload and cut power for more than two hours; clear space while still unpowered and confirm exactly 10 refined biomass appears once. Confirm an incomplete reaction still spoils after two hours without power.
4. Check Do X times and Do until X after both successful and failed batches.
5. Load an existing colony with a refinery built before processor support, and confirm nearby processors can assist it.

### Research

XML tests validate the tab, all foundation prerequisite paths, category generator configuration, building and recipe gates, default research costs, distinct layout positions and introduction translation text. In-game checks: verify locked projects/buildings/bills before research; finish Xenogenetics and check the introduction; research each branch and check its unlocks; save/reload to confirm the introduction does not repeat; also load a colony with Xenogenetics already complete and GeneFoundry newly installed. UI layout, automatic component discovery and popup timing require this manual verification.

Pure unit tests cover the formula, configurable terms, minimum costs, overflow, monotonic increases and all combinations of foundation/category/archite gates. Optional installed-game API tests cover category membership, modded/uncategorized genes, mixed and pure archite prerequisites, stable IDs, actual research database registration, duplicate prevention and vanilla UI coordinate generation. The registration fixture bypasses vanilla DLC discovery in `PostLoad`; definition loading with real DLC and the displayed research tree still require an in-game check. Manually verify category descriptions and costs, the archite-only prerequisite, save/reload of category research, and changed costs after a restart. Save completed and partially researched projects with this version, then add genes to an existing category, add a new category and increase research costs. Completed projects must stay complete; unfinished projects must retain points against the new cost; new categories must start unresearched. Managed regressions round-trip completion IDs with Scribe and replace vanilla completion notifications/DLC discovery in the headless fixture. Older saves without completion records cannot identify research already made partial by changed costs. Confirm the merged foundation unlocks extraction/refinement, Enzymatic extraction follows it, and every category draws a foundation link.

### Fabrication

Runtime acceptance still requires RimWorld. Use a disposable colony to check:

1. Load the mod with Biotech; check the log for XML/cross-reference errors, build the 5×2 workstation and inspect the selection window at the intended UI scale. Confirm the artwork and front interaction position.
2. Verify the gene picker starts collapsed, Show all changes to Hide all, individual headings still toggle, search reveals matching genes, and footer hover details remain readable without overlapping buttons. Try locked foundation/category/Archogenetics gates, cosmetic unlocks and a modded category. Confirm single and multi-gene orders display exact discounted biomass costs, unchanged work and capsule costs. Add several one-pack bills through the same selector; check there are no quantity/repeat controls. Reorder, suspend, edit an inactive entry, and delete one.
3. Split biomass and capsules across stacks, forbid or reserve an ingredient, and check short supplies block starting/work. Confirm each successful order consumes only its displayed costs.
4. Compare below-minimum and qualified workers; verify Research assignment picks up work automatically. Draft during hauling and work, incapacitate an operator, switch priorities, remove a worker from the map and confirm another pawn cannot resume its claimed pack. With another bill queued, verify a second pawn sets the first pack aside and starts that bill. Select both active and parked packs and check intended genes and owner; check the bill owner label. Return the first owner and confirm they retrieve and resume their own pack without paying again. Verify the unfinished sprite remains visible above the workstation during work. Check only active work awards the three skills' XP.
5. Save/reload during partial delivery and manufacturing. Cut power and restore it. Cancel and immediately queue a different order while an old operator is travelling. Confirm no duplicated consumption, lost progress or old-order work. Save/reload with queued and suspended bills plus a claimed unfinished pack; check order, selections, ownership, parked-item bill links and suspension remain intact. Discard both current and parked packs and confirm the bills remain ready for new work, with their genes unchanged and 75% recovery exactly once. Destroy a pack through damage: its bill must remain, without recovering destroyed ingredients. Failure must still recover nothing. Right-click to prioritise fabrication with an idle bench and verify the worker continues through setup and hauling into active work. Confirm old saved bill assignments no longer restrict an unstarted bill and no Assign/Unassign controls appear.
6. Complete with minimum-skilled and expert workers; check failure notifications and exact selected gene contents on success. Haul the result into a Gene Bank and use it in the vanilla Xenogerm Assembler. Obstruct output placement and save/reload before clearing space; confirm exactly one output appears.
7. Load an older colony with a built Fabricator; check idle and already-running Fabricators migrate correctly, get an empty Bills tab when no bills were saved, and existing vats, refineries and research still work. Developer-spawn an unfinished item and inspect/haul it without errors or free production.

### Processing environment

Compare clean/dirty rooms, outside and a missing roof; switch lights
through 0%, 15%, 30% and full light at the interaction cell. Check all extraction
types, clean/change lights during fabrication, and save/reload each process. Refinery
risk should remain fixed after batch start; fabrication should retain earlier risk;
extraction should use completion conditions. Automated tests do not simulate lighting
or room classification in a running colony.

### Artwork

Follow the direction, alignment, layering, interruption and save/load checks in [Gene Vat visuals](GeneVatVisuals.md) and [refinery overlays](BiomassRefineryOverlay.md#verification).
