# GeneFoundry v1.0.0

[![Portable checks](https://github.com/GuySymonds/Rimworld_GeneFoundry/actions/workflows/portable-checks.yml/badge.svg?branch=main&event=push)](https://github.com/GuySymonds/Rimworld_GeneFoundry/actions/workflows/portable-checks.yml)
[![RimWorld 1.6](https://img.shields.io/badge/RimWorld-1.6-blue)](Mod/About/About.xml)
[![Requires Biotech](https://img.shields.io/badge/DLC-Biotech_required-blue)](Mod/About/About.xml)
[![License MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

The workflow badge covers portable tests and script syntax only. Game API, production build, package and in-game checks are separate; see [pipeline scope](docs/Pipelines.md) and the [review and outstanding tasks](docs/Review.md).
GeneFoundry — A RimWorld biotechnology mod that transforms organic matter into genetic resources for refining and fabricating custom genes.

For RimWorld 1.6 with Biotech. The Gene Vat, Biomass Refinery and manual Gene Fabricator are implemented.

## Installation, building and releases

Requires **RimWorld 1.6 and Biotech**. Enable Core, Biotech, then GeneFoundry in the mod list. The public name is GeneFoundry; the established internal package ID remains `GBF.GeneticBiomassFabrication`.

Build a complete, validated local upload package with one command (Windows PowerShell 5.1 or PowerShell 7 on Windows):

```powershell
pwsh -NoProfile -File scripts/Release.ps1
```

Pass `-RimWorldManagedDir 'C:\Path To\RimWorld\RimWorldWin64_Data\Managed'` for another installation, or set the same-named environment variable. The existing local S: drive installation remains the default. Builds use .NET Framework 4.7.2 reference assemblies; tests use the .NET 10 SDK.

The output is **`dist/GeneFoundry`**, ready to copy into the game's local `Mods` directory. Packaging never installs or uploads the mod. Only its fixed staging directory is cleaned, and an existing Workshop publication ID is preserved. Detailed build, verification, publishing, updating and version-bumping instructions are in [docs/RELEASE.md](docs/RELEASE.md). The [Workshop description](docs/workshop-description.txt) uses Steam BBCode; [CHANGELOG.md](CHANGELOG.md) contains the initial release notes.

```text
Mod/                   Checked-in Workshop runtime content
  About/               Metadata, preview and icon
  Defs/                Production XML definitions
  Languages/           Translations
  Textures/            Runtime sprites and animation frames
  LICENSE
Source/                C# projects, tests and source artwork
  Textures/            Source artwork; never included in a release
scripts/               Deployment, artwork and release tooling
dist/GeneFoundry/       Generated upload package, including Assemblies/GeneFoundry.dll
```

For a production build without installation side effects: `dotnet build Source/GeneFoundry/GeneFoundry/GeneFoundry.csproj -c Release -p:DeployMod=false`. `Source/Textures/` is your working artwork directory. Export final PNGs manually into `Mod/Textures/`; builds never modify them. Normal local builds run deployment after the build, even when compilation is already up to date; `scripts/Deploy-Mod.ps1` reads assets from `Mod/`. Do not enable both an old development copy and the packaged mod simultaneously.

## Building costs and power

| Building | Construction materials | Power while switched on |
|---|---|---:|
| Gene Vat | 150 steel, 4 components | 250 W |
| Raw biomass hopper | 30 steel | None |
| Biomass Refinery | 200 steel, 6 components | 100 W idle / 1,000 W processing |
| Gene Fabricator | 250 steel, 8 components, 50 plasteel, 2 advanced components | 500 W |

The Fabricator is the late-game construction investment; its genepack ingredient costs and manufacturing rules are unchanged. Passive hoppers require no components. Vat and Fabricator power draw is constant while switched on, including idle time; the refinery switches between its existing idle and processing loads. Flick unused machines off to save power.

An unassisted 36-hour refinery cycle uses 36 kWh and produces 10 refined biomass on success. This excludes idle draw, processor power and failed batches. At the default 18-hour minimum, the refinery itself uses 18 kWh; assisting processors draw their own power. Power-loss pausing and spoilage after two continuous hours are unchanged.

## Raw biomass extraction

### Storage and degradation

Raw biomass spoils after six days at normal temperature using vanilla rot rules. Refrigeration slows rot and freezing stops it. A biomass hopper provides sealed, refrigerated storage: spoilage pauses while biomass occupies its cell, without power or room cooling. This works even when disconnected from a refinery. Removing biomass resumes its existing rot progress; the hopper never makes old biomass fresh. Hoppers also protect stored contents from environmental deterioration. Outside protected storage, raw biomass retains its exposure deterioration rate of 1 and 75 hit points.

Refined biomass does not rot and has zero environmental deterioration. It can still suffer damage, fire or destruction. Ingredients in an incomplete refinery reaction follow the separate two-hour continuous-power-outage spoilage rule, rather than the loose biomass rot timer. Successful completed batches awaiting output space do not spoil. Existing raw stacks begin using the new rot component when loaded; no historical spoilage can be reconstructed.

The Gene Vat supports these material extraction bills:

| Input | Base raw biomass output (before operator wastage) |
|---|---:|
| 50 raw meat | 20 |
| 50 raw meat + 1 herbal medicine | 25 |
| 50 raw plant food | 8 |
| 50 raw plant food + 1 herbal medicine | 10 |

Each material recipe requires 500 work units, uses Cooking work assignment and allows mixing eligible ingredient types. Processing uses the weighted biomass extraction speed described below. The plant bill uses the vanilla `PlantFoodRaw` category, including modded foods assigned to that category. Use its ingredient filter to protect food reserves. It does not accept meals, meat or arbitrary non-food crops.

Basic and enzymatic corpse extraction bills use a custom job selected through vanilla bill and ingredient selection. They default to animal corpses; other organic corpses require explicit ingredient-filter changes. Mechanoid and drone categories are excluded from both corpse ingredient filters, and runtime checks additionally require organic flesh, rejecting mechanoid/drone races even if a mod classifies them as flesh. No individual species or robot item names are listed. Installed artificial parts are removed medically before biomass extraction.

### Enzymatic crops and corpses

Enzymatic crops consume 50 plant food and 1 herbal medicine for up to 10 biomass, versus 8 without enzymes. Corpses recover 40% basic or 50% enzymatic from material multiplied by 1.10. Each enzymatic corpse costs `max(1, ceil(material / 50))` herbal medicine, using the original material before uplift or decay. Qualification requirements are unchanged.

All decay stages remain eligible. Fresh/rotting/desiccated multipliers are 100%/50%/10%. For 28 material, enzymatic base outputs are 15/7/1; for 560 material, 308/154/30. Wastage applies afterward. Enzymes do not affect salvage.

For corpse bills the workgiver locates the selected body through vanilla ingredient selection, calculates the full catalyst cost, and only schedules work when enough permitted, reachable, reservable herbal medicine exists within the bill's search radius. Vanilla bill collection toils deliver ingredients, including multiple medicine stacks. Medical salvage occurs first. The delivered catalyst is consumed at extraction completion, after checking that the full delivered quantity still exists; no partial catalyst charge is made when the full cost is missing. Interrupted extraction leaves unconsumed catalyst available, while completed medical removals remain completed.

Corpse recipe output is genuinely variable. Their labels say so, and no fixed `products` quantity is declared: this removes the misleading "Products: 1" add-bill tooltip. A dedicated recipe counter keeps Do until X available and counts actual raw biomass in the map and carried by pawns, respecting bill counting filters. The enzyme ingredient tooltip shows the minimum one-medicine requirement; the recipe description explains the scaled cost. Actual cost depends on the selected body, so it cannot be expressed as one fixed recipe quantity.

### Gene Vat skills

Basic meat and plant extraction require Cooking 3 only. All enzymatic and corpse extraction bills require Cooking 3, Medicine 3 and Intellectual 2. Vanilla bill selection enforces these recipe requirements. Artificial-part recovery additionally enforces the relevant surgery's requirements, which may be higher. This lets basic processing train an operator toward the advanced recipes.

All six extraction bills use Cooking 40%, Medicine 40% and Intellectual 20%, with reference levels 10/6/4. These are recovery references, not minimum qualifications. Each skill below reference contributes `weight * max(0, reference - level) / reference` to shortfall S. One roll per completed extraction chooses loss uniformly between 0.5S and S; output is `max(1, floor(baseOutput * (1 - loss)))`. Above-reference skills cannot compensate for another skill's shortfall. Cooking 5 / Medicine 6 / Intellectual 4 gives 10-20% loss: a 25-unit batch yields 20-22.

Speed stays at 100% when all skills are at or below reference. Above reference, each skill independently progresses from its reference to 20 using the same weights: `1 + weightedProgress * (maximumSpeed - 1)`. Default maximum is 200%. General work speed, sight and manipulation still apply. No skill grants extra yield. Vanilla material bill completion uses a cached loss result while retaining normal ingredient consumption and output handling.

Active extraction awards 0.1 total base XP per tick, split using the same weights. Vanilla passion and learning modifiers apply independently, so final earned XP need not retain the exact base proportions. No extraction XP is awarded during hauling. Meat and crop jobs retain vanilla ingredient collection, interruption, bill completion and output handling, with only the work phase extended to award weighted XP. Vanilla single-skill recipe XP is disabled to avoid awarding Cooking twice. Corpse work uses the same speed stat and XP helper.

Artificial-part recovery is separate: its surgical speed, qualifications, chance of success and Medicine XP do not use the extraction weighting. The 100% work-speed timing examples assume reference 10/6/4 skills, normal capacities and no other work-speed modifiers.

### Artificial-part recovery

Recovery identifies artificial parts and salvage items from hediff definitions; it does not list individual implants or species. Clothing and equipment are stripped first. Nested implants are processed before their enclosing artificial limbs. The operator must be capable of Medicine and satisfy the matching installation recipe's skill requirements (or the removal recipe when no installation recipe exists).

Each part uses the matching removal recipe's work and medical operation speed; generic vanilla part-removal work is the fallback. Medical work awards Medicine XP. Success uses the matching installation surgery's outcome-quality components, including surgeon ability, recipe difficulty, applicable inspirations and the vanilla probability cap. The Gene Vat supplies applicable environmental factors such as lighting and being outdoors, with the room cleanliness factor removed. Medicine-quality and patient-bed components are omitted. No medicine, anaesthetic or live-patient injury outcomes are used. Parts without an installation surgery use removal surgery factors.

Each completed attempt physically removes its hediff from the corpse before producing salvage. Failed recovery destroys the artificial part; successful recovery creates the item specified by `spawnThingOnRemoved`. Parts without a defined removal item are discarded. Removing a replacement limb leaves it missing instead of restoring an organic limb, preventing artificial material from becoming biomass. Completed removals therefore cannot be rerolled by interrupting or restarting the job. Interrupted unfinished removal can be attempted again, with no recovery roll until its work completes. Biomass extraction starts after all remaining artificial parts have been handled.

Manual checks: process a corpse with multiple implants; compare low/high qualified medics; confirm no medicine is consumed; compare dirty/clean rooms with identical lighting; interrupt after one removal and verify it does not reappear or produce twice; confirm extraction continues after successful and failed salvage. Mods that supply custom removal effects rather than a standard removal item may need compatibility support.

Corpse base biomass is `max(1, floor(material * 1.10 * efficiency * decay))`, using 0.40 basic or 0.50 enzymatic efficiency. Existing MeatAmount body-size, missing-part, slaughter, nutrition and difficulty adjustments remain, without the small-animal post-processing bonus. Operator wastage applies to this base output. 100 material yields 44 basic or 55 enzymatic before wastage.

Fresh processing time starts at `(setup minutes + material / 50 * extraction minutes) / 2`, divided by the operator's weighted biomass extraction speed. Defaults of 2.4 setup minutes and 24 extraction minutes per 50 material give 1.2 setup minutes and 12 minutes per 50 material before other modifiers. Above 50 material, multiply work by `max(0.75, (material / 50)^-0.1)`: larger bodies gain a gradual batch-efficiency benefit, capped at 25% less work.

Decay then multiplies processing time by `handling share + (1 - handling share) × decay multiplier`. With the default 30% handling share, fresh bodies take 100%, rotting bodies 65% and desiccated bodies 37% of fresh time. This makes a large desiccated body slower to process than a small fresh body yielding the same biomass. Time is captured after delivery/stripping, using the settings, operator speed and corpse condition at that point, and saved per job. Yield uses the condition at completion; a body that decays during work can therefore give less output without shortening a job already underway.

Clothing, equipment and inventory are stripped before extraction. Vanilla pawn butcher-product generation recovers special products, while meat and leather are consumed rather than produced separately. Existing meat and plant bills still use vanilla DoBill jobs through the Gene Vat workgiver; the old vanilla Cooking workgiver patch is replaced to prevent it bypassing the custom corpse job.

### Raw biomass timing options

Open **Options → Mod Settings → GeneFoundry → Raw biomass**. Sliders have hover explanations, and this section has its own reset button:

| Setting | Default | Range | Effect |
|---|---:|---:|---|
| Fresh extraction per 50 material | 24 minutes | 1–120 minutes | Halved to 12 minutes before other modifiers; scales with body size. |
| Setup time per corpse | 2.4 minutes | 0–15 minutes, 0.1-minute steps | Halved to 1.2 minutes before other modifiers; adds per-body work. |
| Work unaffected by decay | 30% | 0–100%, 1% steps | At 0%, time scales entirely with recovery. At 100%, decay does not shorten work. |
| Maximum extraction speed at level 20 | 200% | 150–300%, 1% steps | Skill-speed ceiling at all skills 20; progress starts at each reference. |

The view shows current fresh/rotting/desiccated timing percentages. Corpse duration is captured when extraction starts, while meat/crop speed responds during work. Operator wastage applies at completion to all extraction bills. The corpse-specific timing sliders do not affect meat/crop bills. Settings persist across saves. Refined biomass options are grouped separately below, with an independent reset button.

### Calculation unit tests

The .NET 10 xUnit project compiles the production `BiomassCalculations.cs` source directly, without loading RimWorld or running the deployment script. This tests the exact math used by the mod while keeping the test runner independent of a game installation. It covers yield and rounding, damaged bodies, minimum recovery, decay/handling timing, equal-yield comparisons, batch efficiency, operator speed and configurable minutes.

```powershell
dotnet test Source/GeneFoundry/GeneFoundry.Tests/GeneFoundry.Tests.csproj
```

The same test project also checks all mod definition XML for parsing and surrounding text whitespace, duplicate definition names, unresolved GeneFoundry references, recipe/product reference types and custom classes included in the production project. Regression checks cover Gene Vat recipe/workgiver registration and corpse ingredient safety/decay settings. Fixtures are copied from the real definitions and sources at build time; no game installation is needed for these checks.

The game mod remains .NET Framework 4.7.2. These static checks do not replace RimWorld's definition loading, inheritance and cross-references to vanilla or other mods. UI rendering, job behavior and game-object integration still require in-game testing.

Optional managed game-API regression tests can run with an installed RimWorld 1.6 assembly directory:

```powershell
dotnet test Source/GeneFoundry/GeneFoundry.Tests/GeneFoundry.Tests.csproj '/p:RimWorldManagedDir=S:\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
```

These additional tests use isolated managed game objects without launching Unity. They check the actual public `HaulAIUtility.UpdateJobWithPlacedThings` callback, merged and separate ingredient receipts, missing-receipt completion rejection, and all four rotations of the corpse processing cell. Without this property, the portable calculation/XML suite remains independent of the game installation.

### Ingredient consumption and corpse placement

Vanilla hauling only attaches its ingredient-placement tracking callback to a fixed set of vanilla JobDefs. GeneFoundry's custom material and corpse jobs explicitly provide that same public callback when dropping their bill ingredients. This prevents fixed-output material recipes from creating biomass with an empty ingredient list and leaving inputs reusable. Material completion additionally validates that the full recorded recipe cost is still physically present before invoking vanilla recipe completion and consumption. Enzymatic corpse medicine is likewise consumed from tracked, delivered quantities at completion.

Corpses are placed at the front-centre tile **inside** the Gene Vat footprint, one tile behind its operator's interaction cell:

```text
x x x
x x x
x y x
```

Both corpse bills use this cell during medical recovery and extraction. A corpse already delivered elsewhere among the vat's ingredient cells is positioned there before work; another corpse occupying that spot blocks the job. Moving the processing corpse away interrupts work instead of processing it remotely. Manual regression checks should compare total meat, plant and herb quantities before/after completed batches, confirm catalyst consumption from multiple stacks, and verify corpse positioning and interruption in-game.

Before v1, development prioritises current behavior over compatibility with older development saves. Normal save/load of current jobs and settings remains supported; migration of old development saves is not a release requirement.

## Biomass refinery

Each attempt consumes **40 raw biomass + 1 neutroamine** when preparation finishes. Success produces exactly **10 refined biomass**; failure produces zero with no refund.

Hoppers must touch a side of the refinery and face it. Multiple hoppers combine their stock; ingredient validation, bill counting and hauling remain unchanged. Qualifications remain Intellectual 5, Crafting 3 and Medicine 2. The single weighted skill is `clamp(0.60 * Intellectual + 0.25 * Crafting + 0.15 * Medicine, 5, 20)`.

| Weighted skill | Preparation | Operator failure |
|---|---:|---:|
| 5 | 90 minutes | 30% |
| 10 | 70 minutes | 5% |
| 15 | 50 minutes | 2.5% |
| 20 | 30 minutes | 0% |

Preparation is `90 - 4 * (skill - 5)` minutes; operator failure is piecewise linear: `0.30 - 0.05 * (skill - 5)` from skill 5 to 10, then `0.05 * (20 - skill) / 10` from 10 to 20. No expert yield or preparation bonuses apply. Preparation awards existing weighted XP. Automated processing is independent: **36 hours**, **5% reduction per available gene processor**, **18-hour minimum** by default. Existing processor allocation gives active gene assemblers priority. Power loss pauses work and spoils the batch after two continuous hours. Do X times counts initiated batches; Do until X targets stored output. Completion rolls once and retains the result across save/load and placement retries.

Preparation interrupts when the workstation becomes unusable (including power loss), the bill is suspended/deleted or no longer eligible, or the worker is drafted. No further preparation XP is awarded and no batch ingredients are consumed on interruption. Current-job save/load retains preparation duration and remaining work; it rechecks usability and bill state when toils resume. A cancelled job starts a fresh preparation when scheduled again.

A successful completed batch awaiting output space is shelf-stable, remains saved and blocks new batches until its output is placed. It retries placement even without electricity, uses the refinery's 100 W idle load when powered, and needs no processor assistance. Inspection shows **Output waiting for space**. Outages cannot spoil completed output or cause another failure roll; exactly 10 refined biomass is placed once space becomes available.

### Research progression

All GeneFoundry production is gated behind vanilla **Xenogenetics** (internal definition `Xenogermination`). Projects appear in their own **GeneFoundry** research tab:

| Project | Cost | Prerequisite | Unlocks |
| --- | ---: | --- | --- |
| Biomass extraction | 500 | Xenogenetics | Gene vat and basic meat, plant and corpse extraction |
| Enzymatic extraction | 750 | Biomass extraction | Herbal medicine recipes for meat, plants and corpses |
| Biomass refinement | 1,000 | Biomass extraction | Refinery, raw biomass hoppers and refinement bill |
| Gene fabrication | 1,500 by default | Biomass refinement | Independent projects for each loaded gene category |
| Gene category fabrication | Calculated per category | Gene fabrication; Archogenetics too for non-cosmetic archite-only categories | Fabrication permission for genes in that category |

Enzymatic extraction and refinement are independent branches. Buildings and recipes both have research gates, so an existing vat does not bypass recipe research. The tab remains visible before Xenogenetics so players can inspect the progression, but the projects cannot be researched yet.

A paused introduction dialog explains the production chain once Xenogenetics is complete. It also appears when loading a colony that already researched Xenogenetics before installing the mod. This uses a per-game saved flag and checks during game updates, including while paused; acknowledging it and saving prevents it from repeating on subsequent loads. Translation text is included under `Languages/English/Keyed` and copied by the deployment script.

XML tests validate the tab, all foundation prerequisite paths, category generator configuration, building and recipe gates, default research costs, distinct layout positions and introduction translation text. In-game checks: verify locked projects/buildings/bills before research; finish Xenogenetics and check the introduction; research each branch and check its unlocks; save/reload to confirm the introduction does not repeat; also load a colony with Xenogenetics already complete and GeneFoundry newly installed. UI layout, automatic component discovery and popup timing require this manual verification.

### Gene fabrication research

Cosmetic categories unlock directly once **Gene fabrication** is complete, with no category research project or extra research work. This includes `Cosmetic` and the `Cosmetic_` category family, including modded categories with those stable definition names. Cosmetic archite genes still require vanilla **Archogenetics**. Older automatically completed cosmetic projects remain supported when present, but new cosmetic projects are not generated.

Gene category projects are generated during definition resolution from the loaded `GeneDef.displayCategory` values. This includes modded and generated genes, zero-complexity cosmetic genes and genes excluded from random gene-set generation. Empty categories do not produce projects; genes without a category share an Uncategorized project. Stable project names use category definition names, not translated labels. All category projects remain in the GeneFoundry tab. Their descriptions list their genes and explain their cost breakdown.

Default category research cost:

`clamp(ceil((500 + (100 × gene count) + (200 × total complexity) + (500 × total archite value)) × 25%), 1,000, 4,000)`

Each gene's complexity and archite value are clamped independently to at least zero before summing, so negative modded stats cannot cancel another gene's cost. Each gene definition counts once. Scale the full calculation, round up, then apply the category minimum and maximum. Defaults reduce the original costs to one quarter, bounded between 1,000 and 4,000: the pictured Miscellaneous category falls from 6,800 to 1,700, Aptitudes from 19,700 to 4,000, and Archite from 12,200 to 3,050. Small categories cost at least 1,000. Categories at either limit cost the same despite different contents. Wide arithmetic prevents overflow.

The research view keeps the main progression above a compact four-column category grid. Category labels show only the category name. Category requirements use vanilla `hiddenPrerequisites`: they still prevent research and appear in the selected project's prerequisite details, but do not draw lines across the grid. Category projects remain independent; the layout introduces no extra research prerequisites. Stable definition names preserve research identity despite the new labels and positions.

**Archite genes require vanilla Archogenetics** (`Archogenetics`) as well as the foundation and their category project. An archite gene is identified by `biostatArc > 0`, regardless of category. Mixed categories can be researched without Archogenetics so their ordinary genes become available; their archite genes stay locked. Non-cosmetic archite-only categories also list Archogenetics as a research prerequisite. Cosmetic category completion is automatic, while archite gene fabrication remains gated individually. Setting the archite surcharge to zero does not remove this requirement. Vanilla gene extraction and assembly are unaffected.

The separate **Gene research** settings section provides sliders for the fabrication foundation (100–10,000; default 1,500), category base cost (0–5,000; default 500), cost per gene (0–1,000; default 100), cost per complexity point (0–2,000; default 200) and cost per archite value (0–5,000; default 500). Category cost percentage (1–200%; default 25%), minimum category cost (100–10,000; default 1,000) and maximum category cost (minimum–10,000; default 4,000) apply after these terms. Raising the minimum above the maximum raises the maximum too. These controls do not affect the fabrication foundation or Archogenetics requirements. Missing settings take the new defaults without resetting custom term weights. The section has an independent reset button. Restart RimWorld after changing these settings. Existing research progress remains in points, so increasing costs can make even a formerly completed project require more work. Earlier biomass research costs are unchanged.

The Fabricator uses the shared `GeneFabricationResearch.IsUnlocked(GeneDef)` permission check both when queuing an order and during work. Research completion alone does not create genepacks.

Pure unit tests cover the formula, configurable terms, minimum costs, overflow, monotonic increases and all combinations of foundation/category/archite gates. Optional installed-game API tests cover category membership, modded/uncategorized genes, mixed and pure archite prerequisites, stable IDs, actual research database registration, duplicate prevention and vanilla UI coordinate generation. The registration fixture bypasses vanilla DLC discovery in `PostLoad`; definition loading with real DLC and the displayed research tree still require an in-game check. Manually verify category descriptions and costs, the archite-only prerequisite, save/reload of category research, and changed costs after a restart.

### Settings and testing

Open **Options -> Mod Settings -> GeneFoundry** for automated processing durations (5 minutes to 72 hours), processor assistance, raw extraction maximum speed and corpse timings. Corpse timing parameters are halved in the existing job; their stored defaults remain 24 and 2.4 minutes. Preparation follows the fixed linear formula. Reset buttons restore the new defaults; existing saved useful settings remain configurable. Expert yield, expert preparation and sterile-room bonus controls are removed.

### Refinement cleanliness

Environmental failure is additive at every operator level. Indoors, `clamp(-cleanliness / 2, 0, 1) * 0.15` gives no penalty at 0 or above, +7.5 percentage points at -1 and +15 at -2 or worse. Sterile rooms give no bonus. Outdoor, map-edge or unroofed rooms instead add a fixed 10 points, replacing dirt. Dim light adds up to 10 further points at darkness, tapering to zero at 30% light at the operator interaction cell. `totalFailure = clamp(operatorFailure + environmentFailure, 0, 1)` is captured at batch start. A level-20 operator therefore still has 7.5% risk at cleanliness -1, 15% at -2, or 10% outdoors. The shared processing-environment controls adjust dirt, exposure and darkness penalties. The cleanliness checkbox affects dirt only; exposure and light remain independent. Changes affect new refinery batches only.

Manual verification: compare all input types, qualification levels, clean/dirty/outdoor rooms, hopper orientation, interrupted preparation, power spoilage, processor assistance, save/load during work and blocked-output retries. No in-game validation is claimed by the unit suite.

Managed refinery lifecycle regressions exercise real job failure/learning callbacks and Scribe round-trips: preparation duration/remaining ticks survive loading, then power loss, bill suspension/deletion, drafting and workstation despawn stop eligibility and XP without starting a batch. Completed-output checks cover the first success transition, long outages, save/load, no reroll, idle power/no assistance and exactly-once placement. Incomplete reactions still spoil at precisely 5,000 outage ticks. Headless fixtures replace output placement and player notifications; live-colony scheduling and Unity rendering remain manual checks.

The XML processing duration of 2,500 ticks is retained solely to migrate saves made before settings existed. Runtime processing uses Mod Settings. The developer start-refinement command bypasses operator preparation and operator/dirt risk; exposure and dim-light penalties still apply.

### Final in-game regression checks

Use a disposable save and short processing settings:

1. Run two refineries near shared processors; confirm no processor assists both and excess capacity can assist the second refinery.
2. Start, interrupt and cancel an assembler job. Verify it retains priority during operator breaks and releases processors when its job ends.
3. Save/reload during preparation, assisted processing and a power outage. Check progress, learning, risk and outage time continue correctly. Cut power, suspend/delete a bill or draft its worker during preparation: preparation and XP must stop without charging ingredients. Block completed output, save/reload and cut power for more than two hours; clear space while still unpowered and confirm exactly 10 refined biomass appears once. Confirm an incomplete reaction still spoils after two hours without power.
4. Check Do X times and Do until X after both successful and failed batches.
5. Load an existing colony with a refinery built before processor support, and confirm nearby processors can assist it.

Raw biomass extraction, refinement, gene research and manual gene fabrication are implemented.

## Manual gene fabrication

Build the powered 5×2 Gene Fabricator under **Architect → Biotech** after researching **Gene fabrication**. Open its **Bills** tab and add a fabrication bill, or use **Add fabrication bill**. The searchable window groups loaded vanilla and modded genes by their existing categories, uses vanilla gene icons/info cards, marks selections and shows exact material costs, complexity, estimated duration, skill requirements and failure rules. Choose one or more unlocked genes and confirm one order. No sample is required. The bill opens the same gene selector. There is no separate immediate-fabrication button. Each bill makes exactly one genepack, which can contain multiple genes; there are no quantity or repeat controls. Native bill ordering, suspension, deletion and saving remain available. Details reopens the selector; an active bill cannot have its genes edited. An entry leaves the queue after one completed attempt, successful or failed. Materials need not be present when adding a bill, but the full reachable cost is checked before that entry starts. The selector uses the same vanilla gene-creation base as the Gene Assembler: selected gene cards above a categorized gene library, with vanilla search and info cards. Only researched, manufacturable genes appear. Cosmetic genes, including hair, skin, body and generated/modded cosmetic definitions, are included without requiring physical samples or separate cosmetic research.

Assign a qualified colonist to **Research** work. All three minimums apply: Intellectual 10, Medicine 6 and Crafting 4, with those skills enabled. Operators haul reachable, permitted materials using vanilla carrying and reservations, one stack per trip. The machine reserves a single operator at a time. It holds partial deliveries across interruptions and consumes the complete cost once when manual work begins. Missing ingredients block work; no partial payment creates an unfinished product. Cancellation returns uncommitted delivered ingredients in full. Once paid work begins, cancellation destroys the unfinished item and recovers 75% of its captured Refined Biomass and Archite Capsule costs, rounding each resource down to whole items. Failure still returns nothing. Recovery is recorded once; repeated cancellation cannot duplicate ingredients.

Manufacturing defaults are centralized in `FabricationCalculations.cs`, separate from research costs:

| Parameter | Default |
|---|---|
| Refined biomass | `ceil((10 × gene count + 5 × summed positive complexity) × (100 − discount%)/100)` |
| Multi-gene biomass discount | 10% per additional gene, capped at 50% for six or more genes |
| Required work | `2,500 × gene count + 1,000 × summed positive complexity` |
| Archite capsules | Sum each selected gene's positive vanilla `biostatArc` |
| Power consumption | 500 W |
| Skill speed | `0.5 + (0.60 × Intellectual + 0.25 × Medicine + 0.15 × Crafting) / 10`, with each level clamped to 0–20 |
| Work modifiers | General work speed, manipulation and sight multiply skill speed |
| Active work XP | 0.1 total base XP per active tick, split 60% Intellectual / 25% Medicine / 15% Crafting |

For example, one complexity-2 gene costs 20 biomass and 4,500 work. Three genes with summed positive complexity 5 cost 44 biomass (20% off 55) and 12,500 work. Two genes with summed positive complexity 1 cost 23 biomass, rounding 22.5 upward. The discount applies only to Refined Biomass; work and Archite Capsules are unchanged. Negative modded complexity or archite values cannot discount another gene. These are initial balancing defaults, not new settings. Estimated game hours use 2,500 ticks/hour and minimum-qualified skills with normal capacities and general work speed. Hauling and interruptions add time.

Operator failure probability is `30% × (0.60 × clamp((16 − Intellectual)/6, 0, 1) + 0.25 × clamp((12 − Medicine)/6, 0, 1) + 0.15 × clamp((10 − Crafting)/6, 0, 1))`. Minimum-qualified operators have 30% risk; 16/12/10 or better gives zero operator risk before environmental penalties. The unfinished item saves actual work and the sum of `work × (operator risk + environment penalty)`; final risk is that sum divided by work done, clamped to 0–100%. Final-tick work is capped to the remaining amount, so overshoot cannot inflate an operator's contribution. Fabrication uses the same dirt and exposure penalties as refinement, plus dim light. Conditions are evaluated during active work, so cleaning or improving lighting affects remaining work without rewriting saved risk. There is no XP during hauling, travel or waiting. Vanilla passion and learning modifiers apply to each skill's XP.

Drafting, incapacity, higher-priority jobs, leaving the map and power loss stop active work while the machine retains its paid unfinished item. The pawn who begins actual manufacturing owns the unfinished pack; hauling alone does not claim it. Only that same pawn can resume it, but their paused work no longer locks the machine. The unfinished pack is a real spawned, selectable item, labelled with its intended genes and assigned pawn. Its bill also displays the assigned pawn. Another qualified pawn can reserve the idle workstation, physically set the pack aside, and start the next eligible bill. The owner later retrieves their original pack with its progress and payment intact. Right-click the bench with a qualified pawn enabled for Research and choose the standard Prioritize fabrication option. It starts eligible work immediately and sustains the native priority through bill preparation, hauling and fabrication. There are no per-bill colonist assignments or Assign/Unassign controls. Any qualified pawn can start an unstarted bill; a started pack remains locked to its creator. Existing saved per-bill assignments are ignored. If the owner dies or leaves permanently, cancel the job (or delete its active bill) and start a new entry. Discarding or destroying an unfinished pack keeps its bill and selected genes, clears its ownership and progress, and allows a fresh paid attempt. Explicitly deleting the bill still removes the entry. Existing unowned work from older saves is claimed by the first pawn who resumes it, without resetting progress. The owner reference and a separate ownership flag are saved, so an unavailable reference does not silently unlock the pack. Active work is placed on the workstation as an ordinary selectable, haulable item, drawn 0.06 of a tile left and 0.12 of a tile farther up the bench without changing its physical cell. Adjust `WorkSurfaceOffsetX` and `WorkSurfaceOffsetZ` near the top of `Source/GeneFoundry/GeneFoundry/Fabrication/FabricationState.cs`: negative X moves left, positive X right; positive Z moves up, negative Z down. These are map-tile units, so their apparent pixel distance varies with zoom. Rebuild and deploy the mod, then restart RimWorld after changing them. Native right-click availability and sustained priority are configured by `directOrderable` and `prioritizeSustains` in `Mod/Defs/WorkGiverDefs/GeneFabrication.xml`. Parked work may be hauled into storage and is retrieved through vanilla carrying and reservations. Selected genes, delivered materials, work, weighted risk, bill association and the completion result are serialized. A saved generation marker prevents cancelled jobs from acting on replacement orders. A developer-spawned unfinished item has no paid state and cannot produce a free pack.

Completion rolls failure once. Failure loses the paid materials and unfinished item, sends a notification and clears the order. Success calls vanilla `Genepack.Initialize` with exactly the selected genes on a vanilla genepack. The output stays in the machine's saved ThingOwner if placement is blocked, with periodic placement retries and no second completion or roll. It uses vanilla hauling, Gene Bank and Xenogerm Assembler data. Vanilla extraction and assembly are unchanged.

### Fabrication verification

The portable suite covers deterministic costs, skills, risk and work-weighting, plus actual definition/source registration. The optional installed-game suite additionally exercises real unfinished state, partial delivery accounting, exact discounted commitment (retaining excess delivered materials), repeated commitment, pawn ownership, overrun clamping, invalid developer state, cancellation, inactive work rejection, success/failure and vanilla multi-gene initialization. Scribe tests round-trip the job fields and actual owner reference through saving, loading, cross-reference resolution and post-load initialization, plus bill genes and completed state. Bill lifecycle tests cover ordering, suspension, exactly-once removal on success/failure and deleting an active bill. Headless fixtures replace Unity object creation, destruction notifications and map placement; they do not simulate a running colony or a full save-load cycle.

Runtime acceptance still requires RimWorld. Use a disposable colony to check:

1. Load the mod with Biotech; check the log for XML/cross-reference errors, build the 5×2 workstation and inspect the selection window at the intended UI scale. Confirm the artwork and front interaction position.
2. Try locked foundation/category/Archogenetics gates, cosmetic unlocks and a modded category. Confirm single and multi-gene orders display exact discounted biomass costs, unchanged work and capsule costs. Add several one-pack bills through the same selector; check there are no quantity/repeat controls. Reorder, suspend, edit an inactive entry, and delete one.
3. Split biomass and capsules across stacks, forbid or reserve an ingredient, and check short supplies block starting/work. Confirm each successful order consumes only its displayed costs.
4. Compare below-minimum and qualified workers; verify Research assignment picks up work automatically. Draft during hauling and work, incapacitate an operator, switch priorities, remove a worker from the map and confirm another pawn cannot resume its claimed pack. With another bill queued, verify a second pawn sets the first pack aside and starts that bill. Select both active and parked packs and check intended genes and owner; check the bill owner label. Return the first owner and confirm they retrieve and resume their own pack without paying again. Verify the unfinished sprite remains visible above the workstation during work. Check only active work awards the three skills' XP.
5. Save/reload during partial delivery and manufacturing. Cut power and restore it. Cancel and immediately queue a different order while an old operator is travelling. Confirm no duplicated consumption, lost progress or old-order work. Save/reload with queued and suspended bills plus a claimed unfinished pack; check order, selections, ownership, parked-item bill links and suspension remain intact. Discard both current and parked packs and confirm the bills remain ready for new work, with their genes unchanged and 75% recovery exactly once. Destroy a pack through damage: its bill must remain, without recovering destroyed ingredients. Failure must still recover nothing. Right-click to prioritise fabrication with an idle bench and verify the worker continues through setup and hauling into active work. Confirm old saved bill assignments no longer restrict an unstarted bill and no Assign/Unassign controls appear.
6. Complete with minimum-skilled and expert workers; check failure notifications and exact selected gene contents on success. Haul the result into a Gene Bank and use it in the vanilla Xenogerm Assembler. Obstruct output placement and save/reload before clearing space; confirm exactly one output appears.
7. Load an older colony with a built Fabricator; check idle and already-running Fabricators migrate correctly, get an empty Bills tab when no bills were saved, and existing vats, refineries and research still work. Developer-spawn an unfinished item and inspect/haul it without errors or free production.

No in-game runtime verification is claimed by the automated suite. Build with `-p:DeployMod=false` to skip the repository's deployment target when verification should not modify the installed mod.

### Corpse liquidation visuals

During actual corpse extraction, the corpse visually advances through fresh, rotting and desiccated artwork at one-third and two-thirds progress. An already decayed corpse never looks fresher. Actual rot, yield, work, placement and ingredient handling are unchanged.

Presentation measures the native render meshes with their laying orientation, node and life-stage offsets. Small corpses keep their natural scale; oversized corpses shrink proportionally using twice the basin width and height as their mesh fit allowance; the natural-size cap remains. This relaxed allowance compensates for sprite padding and can let large silhouettes extend beyond the basin. The pose and one fit scale are cached for the work session across all three rot appearances. Each appearance is centred on the same basin centre. Mesh bounds are conservative and include transparent texture padding; no biological body-size guesses or GPU texture readbacks are used. The corpse's map position is unchanged.

Eight final BubbleMass textures live in `Mod/Textures/GeneFoundry/Effects/BubbleMass` and are copied unchanged during deployment. The overlay uses alpha blending, preserves the 500:203 aspect ratio and sits 0.06 tiles inside the basin edge nearest the operator in every orientation. It loops forward 00 through 07 at 6 game ticks per frame (48 ticks per cycle), above the corpse and below ordinary item/pawn layers. Each work session has its own tick origin. Pausing freezes playback. Hauling, stripping and part recovery do not show the overlay. Active meat and crop extraction also show the bubbles, using the same cached resources. Interruptions and power loss remove it; normal corpse drawing returns. Save/load rebuilds this transient presentation from the active job.

The native Gene Vat graphic uses `BuildingOnTop`, above vanilla walls' `Building` layer, for both idle and animated materials. Its footprint, draw size, interaction cell and directional images are unchanged. The vial animation keeps its existing sequence and timing.

Manual checks still required in RimWorld: process rats, hares, humans, muffalo, thrumbo and an oversized modded animal in different laying orientations, including already decayed bodies. Check centring and constant size through both rot transitions, alpha blending and containment of bubbles, exposed metal edges and the underlying vial animation. Interrupt/draft, cut power, finish work and save/reload; confirm no doubled or missing corpse. Run two vats with staggered start times. Compare idle and active vats against a northern wall and away from walls, checking nearby pawn/item overlap. Automated tests cover fit calculations, transformed mesh bounds and centring, animation timing, independent presentation lifecycle, asset identity and definition/layer wiring; they do not verify Unity rendering.

### Gene Vat rot stink

During active meat, plant or corpse extraction (normal or enzymatic, at every decay stage), the current operator has a 250-tick incident check: Cooking, Medicine and Intellectual each add 5% risk when below level 10 (0/5/10/15% total). Hauling, idle time, stripping and implant recovery do not make checks. Partial check progress is retained on the vat across work sessions; active incidents suspend checks.

Each vat persists its own incident deadline and emission schedule. An incident lasts 2,500 game ticks despite interruptions, operator replacement or power loss. It emits 50 vanilla rot stink gas units at the corpse cell immediately and every 250 ticks thereafter (10 releases, 500 total). Despawned/destroyed vats emit nothing; despawned time still counts toward expiry. Existing gas disperses naturally. Vanilla controls visuals, diffusion, dissipation, exposure and lung rot; natural corpse gas is unchanged. The inspection warning appears only while an incident is active.

### Gene Vat machine animation

The rotatable vat selects directional idle artwork through its custom Graphic_Multi subclass until a pawn reaches actual biomass extraction. Meat, plant and corpse extraction use eight aligned runtime frames per direction, held for 6 game ticks each, in the sequence 00–07–01 (14 displayed frames, 84 ticks). Pausing freezes the frame; game speed controls playback. Each vat starts its own visual cycle. Hauling and artificial-part recovery remain static. The base selects its directional sprite without rotating that artwork; the transition layer selects the matching directional frame and draws it on the same mesh without an extra rotation. Its bubbles remain behind the animal, with the separate foreground BubbleMass layer near the operator. Basin contents and foreground bubbles use measured bounds for each view and follow its processing orientation. Explicit material queues order transitions, corpse/ingredients and foreground BubbleMass in that order. Missing animation assets log once and fall back to the static sprite. No animation state is saved.

In-game checks still required: exercise all six extraction bills, check idle/hauling/part recovery, draft or interrupt the worker, cut power, finish a bill, and save/reload during work. Check two vats with different start times. Confirm alpha edges, alignment, dimensions, corpse placement/liquidation and selection/placement previews remain correct. Automated tests verify timing, sequence, definition wiring and runtime PNG dimensions, transparency formats and frame counts; they do not verify Unity rendering.

Material extraction draws delivered meat/crop stacks inside the lower basin beneath the bubbles, shifted 0.25 cells towards the top so more of the ingredients remain visible. Herbal medicine is hidden during both material and corpse processing, without changing its consumption or position. Ingredient graphics return on interruption; map positions and tracked quantities are unchanged. Biomass extraction work is halved for meat, crops and corpses; artificial-part recovery and animation cadence are unchanged.

Final Gene Vat artwork belongs in `Mod/Textures/GeneFoundry/Buildings/`: provide `GeneVat_north.png`, `GeneVat_east.png`, `GeneVat_south.png`, and `GeneVat_west.png`. Keep the XML graphic class as `GeneFoundry.Recipes.Graphic_GeneVat` to retain animation; it inherits `Graphic_Multi`. Export eight frames per direction into `GeneVatFrames/`, named `vat_00_north.png` through `vat_07_north.png`, and likewise `_east`, `_south`, and `_west`. Each frame must use the exact canvas dimensions and alignment of its directional base (currently 1086x1448 north/south and 1448x1086 east/west). Directional frames are not rotated again. A missing or incomplete set leaves that direction static and logs an error; release validation requires all 32 frames. Basin bounds for job contents and foreground bubbles are measured per view in `MapComponent_CorpseLiquidationVisuals.BasinBounds`. Keep working files in `Source/Textures/`; nothing copies from that directory automatically.
