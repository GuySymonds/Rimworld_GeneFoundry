# Rimworld_GeneFoundry
GeneFoundry — A RimWorld biotechnology mod that transforms organic matter into genetic resources for refining and fabricating custom genes.

For RimWorld 1.6 with Biotech. The Gene Vat and Biomass Refinery are implemented; the Gene Fabricator is planned.

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

The next development focus is finishing the Gene Vat/raw biomass stage. Gene Fabricator implementation remains deferred.
