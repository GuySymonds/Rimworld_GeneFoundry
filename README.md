# Rimworld_GeneFoundry
GeneFoundry — A RimWorld biotechnology mod that transforms organic matter into genetic resources for refining and fabricating custom genes.

For RimWorld 1.6 with Biotech. The Gene Vat, Biomass Refinery and manual Gene Fabricator are implemented.

## Raw biomass extraction

The Gene Vat supports these material extraction bills:

| Input | Base raw biomass output (before expert bonus) |
|---|---:|
| 50 raw meat | 25 |
| 50 raw meat + 1 herbal medicine | 38 |
| 50 raw plant food | 10 |
| 50 raw plant food + 1 herbal medicine | 15 |

Each material recipe requires 1,000 work units, uses Cooking work assignment and allows mixing eligible ingredient types. Processing uses the weighted biomass extraction speed described below. The plant bill uses the vanilla `PlantFoodRaw` category, including modded foods assigned to that category. Use its ingredient filter to protect food reserves. It does not accept meals, meat or arbitrary non-food crops.

Basic and enzymatic corpse extraction bills use a custom job selected through vanilla bill and ingredient selection. They default to animal corpses; other organic corpses require explicit ingredient-filter changes. Mechanoid and drone categories are excluded from both corpse ingredient filters, and runtime checks additionally require organic flesh, rejecting mechanoid/drone races even if a mod classifies them as flesh. No individual species or robot item names are listed. Installed artificial parts are removed medically before biomass extraction.

### Enzymatic crops and corpses

Enzymatic crops consume 50 raw plant food and 1 herbal medicine for a base yield of 15 biomass, versus 10 without enzymes. Enzymatic corpses multiply recovery by 1.5 before final rounding, then apply expert recovery. Each corpse costs `max(1, ceil(material / 50))` herbal medicine. The material basis is the same body-size/remaining-material calculation used for extraction; catalyst cost does not decrease for rotting or desiccated bodies. Both enzymatic bills require Cooking 3, Medicine 3 and Intellectual 2.

All decay stages remain eligible: freshness changes recovery and processing time as usual. Enzymes do not multiply special butcher products or artificial-part salvage. An intact, cleanly slaughtered adult rat (28 material) costs 1 herbal medicine and yields 26/13/2 biomass when fresh/rotting/desiccated before expert bonuses. An equivalent thrumbo (560 material) costs 12 and yields 525/262/52. Damage, nutrition, age and difficulty can change material quantities.

For corpse bills the workgiver locates the selected body through vanilla ingredient selection, calculates the full catalyst cost, and only schedules work when enough permitted, reachable, reservable herbal medicine exists within the bill's search radius. Vanilla bill collection toils deliver ingredients, including multiple medicine stacks. Medical salvage occurs first. The delivered catalyst is consumed at extraction completion, after checking that the full delivered quantity still exists; no partial catalyst charge is made when the full cost is missing. Interrupted extraction leaves unconsumed catalyst available, while completed medical removals remain completed.

Corpse recipe output is genuinely variable. Their labels say so, and no fixed `products` quantity is declared: this removes the misleading "Products: 1" add-bill tooltip. A dedicated recipe counter keeps Do until X available and counts actual raw biomass in the map and carried by pawns, respecting bill counting filters. The enzyme ingredient tooltip shows the minimum one-medicine requirement; the recipe description explains the scaled cost. Actual cost depends on the selected body, so it cannot be expressed as one fixed recipe quantity.

### Gene Vat skills

Basic meat and plant extraction require Cooking 3 only. All enzymatic and corpse extraction bills require Cooking 3, Medicine 3 and Intellectual 2. Vanilla bill selection enforces these recipe requirements. Artificial-part recovery additionally enforces the relevant surgery's requirements, which may be higher. This lets basic processing train an operator toward the advanced recipes.

All six extraction bills use Cooking 40%, Medicine 40% and Intellectual 20%. Levels 0–10 determine base speed: `0.5 + weighted level / 10`. Levels 11–20 contribute independently to an expert fraction: the weighted fraction of those ten extra levels completed. This fraction adds speed between the fixed 150% at level 10 and the configurable expert maximum (200% by default). Balanced operators therefore work at 100% at 5/5/5, 150% at 10/10/10, 175% at 15/15/15 and 200% at 20/20/20 with defaults. Each skill caps independently at 20. General work speed, sight and manipulation modify this skill multiplier; these are not additional Cooking-skill bonuses.

The expert fraction also grants up to 20% extra biomass by default, with no yield bonus at level 10 or below. At level 15 in all three skills the bonus is 10%; at 20 it is 20%. Meat/crop recipes use vanilla recipe efficiency and upward whole-item rounding (default expert outputs: 25 → 30, 38 → 46, 10 → 12). The efficiency factor is biased down by one representable float to prevent floating-point error granting an extra item at an exact integer boundary. Corpse yield applies the expert multiplier after material and decay calculations but before its existing downward rounding and minimum of one. Horns, tusks and artificial-part salvage are not multiplied. Yield is evaluated at completion, so current skill levels and settings apply.

Active extraction awards 0.1 total base XP per tick, split using the same weights. Vanilla passion and learning modifiers apply independently, so final earned XP need not retain the exact base proportions. No extraction XP is awarded during hauling. Meat and crop jobs retain vanilla ingredient collection, interruption, bill completion and output handling, with only the work phase extended to award weighted XP. Vanilla single-skill recipe XP is disabled to avoid awarding Cooking twice. Corpse work uses the same speed stat and XP helper.

Artificial-part recovery is separate: its surgical speed, qualifications, chance of success and Medicine XP do not use the extraction weighting. The 100% work-speed timing examples assume 5/5/5 skills, normal capacities and no other work-speed modifiers.

### Artificial-part recovery

Recovery identifies artificial parts and salvage items from hediff definitions; it does not list individual implants or species. Clothing and equipment are stripped first. Nested implants are processed before their enclosing artificial limbs. The operator must be capable of Medicine and satisfy the matching installation recipe's skill requirements (or the removal recipe when no installation recipe exists).

Each part uses the matching removal recipe's work and medical operation speed; generic vanilla part-removal work is the fallback. Medical work awards Medicine XP. Success uses the matching installation surgery's outcome-quality components, including surgeon ability, recipe difficulty, applicable inspirations and the vanilla probability cap. The Gene Vat supplies applicable environmental factors such as lighting and being outdoors, with the room cleanliness factor removed. Medicine-quality and patient-bed components are omitted. No medicine, anaesthetic or live-patient injury outcomes are used. Parts without an installation surgery use removal surgery factors.

Each completed attempt physically removes its hediff from the corpse before producing salvage. Failed recovery destroys the artificial part; successful recovery creates the item specified by `spawnThingOnRemoved`. Parts without a defined removal item are discarded. Removing a replacement limb leaves it missing instead of restoring an organic limb, preventing artificial material from becoming biomass. Completed removals therefore cannot be rerolled by interrupting or restarting the job. Interrupted unfinished removal can be attempted again, with no recovery roll until its work completes. Biomass extraction starts after all remaining artificial parts have been handled.

Manual checks: process a corpse with multiple implants; compare low/high qualified medics; confirm no medicine is consumed; compare dirty/clean rooms with identical lighting; interrupt after one removal and verify it does not reappear or produce twice; confirm extraction continues after successful and failed salvage. Mods that supply custom removal effects rather than a standard removal item may need compatibility support.

Corpse biomass is `max(1, floor(material amount × 0.5 × 1.25 × decay × expert yield multiplier))`. Material amount uses vanilla MeatAmount with post-processing disabled: body size, missing natural parts, slaughter condition, nutrition and difficulty still apply, but vanilla's small-animal meat bonus does not. The whole-body multiplier includes the remaining organs, bones and hide even when limbs are missing. Decay multipliers are 1 for fresh, 0.5 for rotting and 0.1 for desiccated bodies. Every eligible corpse yields at least one biomass.

Fresh processing time starts at `setup minutes + material / 50 × extraction minutes`, divided by the operator's weighted biomass extraction speed. Defaults of 2.4 setup minutes and 24 extraction minutes per 50 material preserve the original `100 + 20 × material` work baseline. Above 50 material, multiply work by `max(0.75, (material / 50)^-0.1)`: larger bodies gain a gradual batch-efficiency benefit, capped at 25% less work.

Decay then multiplies processing time by `handling share + (1 - handling share) × decay multiplier`. With the default 30% handling share, fresh bodies take 100%, rotting bodies 65% and desiccated bodies 37% of fresh time. This makes a large desiccated body slower to process than a small fresh body yielding the same biomass. Time is captured after delivery/stripping, using the settings, operator speed and corpse condition at that point, and saved per job. Yield uses the condition at completion; a body that decays during work can therefore give less output without shortening a job already underway.

Clothing, equipment and inventory are stripped before extraction. Vanilla pawn butcher-product generation recovers special products, while meat and leather are consumed rather than produced separately. Existing meat and plant bills still use vanilla DoBill jobs through the Gene Vat workgiver; the old vanilla Cooking workgiver patch is replaced to prevent it bypassing the custom corpse job.

### Raw biomass timing options

Open **Options → Mod Settings → GeneFoundry → Raw biomass**. Sliders have hover explanations, and this section has its own reset button:

| Setting | Default | Range | Effect |
|---|---:|---:|---|
| Fresh extraction per 50 material | 24 minutes | 1–120 minutes | Scales extraction work with body size; lower values speed processing. |
| Setup time per corpse | 2.4 minutes | 0–15 minutes, 0.1-minute steps | Adds per-body work before other modifiers; discourages processing many tiny bodies for better throughput. |
| Work unaffected by decay | 30% | 0–100%, 1% steps | At 0%, time scales entirely with recovery. At 100%, decay does not shorten work. |
| Maximum extraction speed at level 20 | 200% | 150–300%, 1% steps | Expert skill-speed ceiling; 150% disables speed gains above level 10. |
| Maximum extra biomass at level 20 | +20% | 0–50%, 1% steps | Expert yield ceiling for all extraction bills; 0% disables the yield bonus. |

The view shows current fresh/rotting/desiccated timing percentages. Corpse duration is captured when extraction starts, while meat/crop speed responds during work. Expert yield settings apply at completion to all extraction bills. The corpse-specific timing sliders do not affect meat/crop bills. Settings persist across saves. Refined biomass options are grouped separately below, with an independent reset button.

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

Each attempt consumes **10 raw biomass + 1 neutroamine** when preparation finishes. A successful batch produces a base yield of **2 refined biomass**, with expert recovery described below. Failed batches produce nothing and do not refund inputs.

- Build hoppers along any side of a completed refinery with their outlets facing it. Diagonal hoppers do not connect. Keep the interaction cell clear. Multiple hoppers combine their stored biomass without speeding production.
- Assign an operator to Research work and add a refinement bill. The operator collects neutroamine, prepares the batch and then leaves automated processing to the machine.
- Minimum skills are Intellectual 5, Crafting 3 and Medicine 2. Operator skill-based failure risk reaches zero at 10, 8 and 6 respectively; dirt or exposure can still add risk. Their weights are 60%, 25% and 15%.
- At default settings, preparation scales from 60 minutes at minimum skills to 30 minutes at the zero-failure thresholds, then to about 22.5 minutes at 20/20/20. Above each threshold, that skill contributes independently toward the expert reward, capped at 20. The same relative 150%-to-200% speed progression as raw extraction gives up to 33.3% extra preparation speed, or 25% shorter preparation compared with 10/8/6. Operator skill-based failure probability remains zero above the thresholds; room conditions can still add risk.
- Expert yield reaches +20% at 20/20/20 by default: a successful batch produces 2 items plus a 40% chance of one extra, for an average of 2.4. Fractional yield uses chance-based whole-item rounding instead of guaranteeing 3 items and inflating the intended bonus. Expected output and failure risk are captured when processing starts. Both the success/failure roll and whole-item yield result are retained if output placement needs retrying. Expert skills do not shorten automated processing.
- Preparation awards 0.1 total base XP per tick, split across the three skills using the same weights. Vanilla passion and learning modifiers apply. Interrupted preparation retains earned XP; hauling and automated processing award none.
- Default processing takes 12 in-game hours. Each available gene processor removes 5% of the base duration; ten reach the six-hour minimum. Reductions add together rather than compound.
- Powered processors must satisfy vanilla facility range and line-of-sight checks. An assembler with an active job takes priority even between operator work sessions. Otherwise, each processor assists the nearest active, powered refinery that can still benefit; ties use building ID. Assistance updates every 60 game ticks.
- Changing assistance adjusts future processing speed while preserving completed progress. Extra processors beyond a refinery's useful limit remain available to other refineries.
- Idle power is 100 W; processing power is 1,500 W. Power loss pauses processing. A continuous 5,000-tick outage (two in-game hours) destroys the batch; restoring power resets the timer.
- **Do X times** counts attempts when processing starts. **Do until X** uses vanilla output-count rules and continues requesting batches until its target is reached.
- Successful completion rolls only once, including when output placement must be retried. Preparation duration, batch progress, failure probability, expected yield, chosen output count, completion-roll state and outage time are saved.

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

Open **Options → Mod Settings → GeneFoundry** to change preparation durations (1–120 minutes), processing durations (5 minutes–24 hours) and processor reduction (0–25%). Minimum durations cannot exceed base durations. Processing changes affect running batches; preparation changes apply to new preparation jobs. Settings are shared across saves. Restore defaults after accelerated testing if desired.

The Refined Biomass section also has independent expert controls: extra preparation speed at level 20 (+0–100%, default +33.3%) and extra output at level 20 (+0–50%, default +20%). Set either to zero to disable that reward. The preparation-time slider for 10/8/6 specifies the pre-expert duration, rather than an absolute minimum. The view previews the resulting level-20 preparation time. Yield settings affect newly started batches; running batches retain their captured expected output. Refined defaults can be restored without changing raw extraction settings.

### Refinement cleanliness

Room cleanliness affects refinement failure risk by default. The refinery reads the game's Cleanliness room stat, rather than checking floor types, so modded floors and cleaning systems can contribute normally. The effects are additive percentage points:

| Condition | Default adjustment |
|---|---:|
| Enclosed, fully roofed room at cleanliness -2 or worse | +15 points |
| Cleanliness -1 | +7.5 points |
| Cleanliness 0 | No adjustment |
| Cleanliness +0.3 | -2.5 points |
| Cleanliness +0.6 or better | -5 points |
| Outdoor, map-edge or partly unroofed room | +15 points instead of the cleanliness adjustment |

Intermediate cleanliness values interpolate linearly. Final failure probability is `clamp(operator risk + room adjustment, 0, 1)`. For example, an operator with 12% risk has 27% risk in a very dirty room, 12% at cleanliness zero and 7% at +0.6. Even an expert with zero operator risk has 15% risk in the very dirty room. No environmental penalty changes preparation, automated processing time or output quantity.

Refined Biomass options include an enable checkbox and independent sliders for the maximum dirty-room penalty (0–50 points), sterile-room reduction (0–30 points), and exposure penalty (0–50 points). The fixed cleanliness breakpoints are -2, 0 and +0.6. All environment values and the final risk are captured when processing starts and saved with the batch; later cleaning, dirt, roof changes or settings changes do not alter an existing batch. The inspect panel shows the operator risk, captured environment adjustment and final batch risk. A ready refinery previews its current room adjustment. Developer-started zero-risk batches bypass cleanliness as well as operator preparation.

Manual checks: use normal bills to compare dirty, clean and sterile rooms with the same operator; remove a roof tile and check the exposure penalty replaces the room effect; clean or change settings after a batch starts and verify its captured risk remains unchanged; disable cleanliness influence and confirm a newly started batch uses operator risk alone. Artificial-part salvage still ignores cleanliness and is unaffected by these settings.

The XML processing duration of 2,500 ticks is retained solely to migrate saves made before settings existed. Runtime processing uses Mod Settings. The developer start-refinement command bypasses operator preparation and starts a zero-risk batch.

### Final in-game regression checks

Use a disposable save and short processing settings:

1. Run two refineries near shared processors; confirm no processor assists both and excess capacity can assist the second refinery.
2. Start, interrupt and cancel an assembler job. Verify it retains priority during operator breaks and releases processors when its job ends.
3. Save/reload during preparation, assisted processing and a power outage. Check progress, learning, risk and outage time continue correctly.
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

Operator failure probability is `30% × (0.60 × clamp((16 − Intellectual)/6, 0, 1) + 0.25 × clamp((12 − Medicine)/6, 0, 1) + 0.15 × clamp((10 − Crafting)/6, 0, 1))`. Minimum-qualified operators have 30% risk; 16/12/10 or better gives zero risk. The unfinished item saves actual work and the sum of `work × operator risk`; final risk is that sum divided by work done, clamped to 0–30%. Final-tick work is capped to the remaining amount, so overshoot cannot inflate an operator's contribution. There is no cleanliness penalty and no XP during hauling, travel or waiting. Vanilla passion and learning modifiers apply to each skill's XP.

Drafting, incapacity, higher-priority jobs, leaving the map and power loss stop active work while the machine retains its paid unfinished item. The pawn who begins actual manufacturing owns the unfinished pack; hauling alone does not claim it. Only that same pawn can resume it, but their paused work no longer locks the machine. The unfinished pack is a real spawned, selectable item, labelled with its intended genes and assigned pawn. Its bill also displays the assigned pawn. Another qualified pawn can reserve the idle workstation, physically set the pack aside, and start the next eligible bill. The owner later retrieves their original pack with its progress and payment intact. Right-click the bench with a qualified pawn enabled for Research and choose the standard Prioritize fabrication option. It starts eligible work immediately and sustains the native priority through bill preparation, hauling and fabrication. There are no per-bill colonist assignments or Assign/Unassign controls. Any qualified pawn can start an unstarted bill; a started pack remains locked to its creator. Existing saved per-bill assignments are ignored. If the owner dies or leaves permanently, cancel the job (or delete its active bill) and start a new entry. Discarding or destroying an unfinished pack keeps its bill and selected genes, clears its ownership and progress, and allows a fresh paid attempt. Explicitly deleting the bill still removes the entry. Existing unowned work from older saves is claimed by the first pawn who resumes it, without resetting progress. The owner reference and a separate ownership flag are saved, so an unavailable reference does not silently unlock the pack. Active work is placed on the workstation as an ordinary selectable, haulable item, drawn 0.06 of a tile left and 0.12 of a tile farther up the bench without changing its physical cell. Adjust `WorkSurfaceOffsetX` and `WorkSurfaceOffsetZ` near the top of `Source/GeneFoundry/GeneFoundry/Fabrication/FabricationState.cs`: negative X moves left, positive X right; positive Z moves up, negative Z down. These are map-tile units, so their apparent pixel distance varies with zoom. Rebuild and deploy the mod, then restart RimWorld after changing them. Native right-click availability and sustained priority are configured by `directOrderable` and `prioritizeSustains` in `Defs/WorkGiverDefs/GeneFabrication.xml`. Parked work may be hauled into storage and is retrieved through vanilla carrying and reservations. Selected genes, delivered materials, work, weighted risk, bill association and the completion result are serialized. A saved generation marker prevents cancelled jobs from acting on replacement orders. A developer-spawned unfinished item has no paid state and cannot produce a free pack.

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

No in-game runtime verification is claimed by the automated suite. Build with `/p:PostBuildEvent=` to skip the repository's deployment script when verification should not modify the installed mod.

### Corpse liquidation visuals

During the Gene Vat extraction phase, the corpse visually advances through fresh, rotting and dessicated artwork at one-third and two-thirds progress. An already decayed corpse never looks fresher. Its rendered width and height shrink linearly from 100% to 50% over that extraction phase. Artificial-part recovery and ingredient hauling do not trigger the effect.

These are presentation changes only. Actual rot progress, race/body size, meat recovery, timing, biomass yield, catalyst requirements and salvage are unchanged. Natural environmental decay still follows the existing rules. Rendering uses loaded vanilla/modded life-stage corpse graphics and native pawn render-node transforms; it does not alter CompRottable or regenerate textures. Normal corpse drawing is restored when extraction is interrupted. A map component discovers active extraction jobs and renders their corpses, including vats saved as vanilla Building_WorkTable; no vat replacement or bill migration is required. The former custom vat type is retained only for compatibility with saves that already contain it. After resuming or loading, visuals follow the current saved extraction progress.

Manual checks: process fresh, rotten and dessicated animals and a humanlike corpse; check the two visual transitions and progressive shrink, including a large animal and a vat built before installing the visual feature. Draft the operator, cut power, remove the vat and save/reload during extraction; confirm ordinary drawing returns when work stops, with no doubled or invisible corpse. Compare biomass yield and salvage to the same inputs without a visual effect. Automated tests cover visual stage/scale boundaries and existing extraction calculations; they do not verify Unity rendering.

### Gene Vat rot stink

During actual corpse dissolution (normal or enzymatic, at every decay stage), the current operator has a 250-tick incident check: Cooking, Medicine and Intellectual each add 5% risk when below level 10 (0/5/10/15% total). Hauling, stripping, implant recovery and meat/plant extraction do not make checks. Partial check progress is retained on the vat across work sessions; active incidents suspend checks.

Each vat persists its own incident deadline and emission schedule. An incident lasts 2,500 game ticks despite interruptions, operator replacement or power loss. It emits 50 vanilla rot stink gas units at the corpse cell immediately and every 250 ticks thereafter (10 releases, 500 total). Despawned/destroyed vats emit nothing; despawned time still counts toward expiry. Existing gas disperses naturally. Vanilla controls visuals, diffusion, dissipation, exposure and lung rot; natural corpse gas is unchanged. The inspection warning appears only while an incident is active.
