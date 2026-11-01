# Rimworld_GeneFoundry
GeneFoundry — A RimWorld biotechnology mod that transforms organic matter into genetic resources for refining and fabricating custom genes.

For RimWorld 1.6 with Biotech. The Gene Vat and Biomass Refinery are implemented; the Gene Fabricator is planned.

## Raw biomass extraction

The Gene Vat supports these material extraction bills:

| Input | Raw biomass output |
|---|---:|
| 50 raw meat | 25 |
| 50 raw meat + 1 herbal medicine | 38 |
| 50 raw plant food | 10 |

Each material recipe requires 1,000 work units, uses Cooking work assignment and allows mixing eligible ingredient types. Processing uses the weighted biomass extraction speed described below. The plant bill uses the vanilla `PlantFoodRaw` category, including modded foods assigned to that category. Use its ingredient filter to protect food reserves. It does not accept meals, meat or arbitrary non-food crops.

An additional corpse extraction bill uses a custom job selected through vanilla bill and ingredient selection. It defaults to animal corpses; other organic corpses require explicit ingredient-filter changes. Non-organic corpses are excluded. Installed artificial parts are removed medically before biomass extraction.

### Gene Vat skills

Basic meat and plant extraction require Cooking 3 only. Enzymatic and corpse extraction require Cooking 3, Medicine 3 and Intellectual 2. Vanilla bill selection enforces these recipe requirements. Artificial-part recovery additionally enforces the relevant surgery's requirements, which may be higher. This lets basic processing train an operator toward the advanced recipes.

All four extraction bills use Cooking 40%, Medicine 40% and Intellectual 20%. Each skill is clamped independently to 0–10, then the weighted level determines speed: `0.5 + weighted level / 10`. Balanced operators at 0/0/0 process at 50%, at 5/5/5 at 100%, and at 10/10/10 at 150%. Higher skills provide no further extraction-speed bonus. General work speed, sight and manipulation modify this skill multiplier; these are not additional Cooking-skill bonuses.

Active extraction awards 0.1 total base XP per tick, split using the same weights. Vanilla passion and learning modifiers apply independently, so final earned XP need not retain the exact base proportions. No extraction XP is awarded during hauling. Meat and crop jobs retain vanilla ingredient collection, interruption, bill completion and output handling, with only the work phase extended to award weighted XP. Vanilla single-skill recipe XP is disabled to avoid awarding Cooking twice. Corpse work uses the same speed stat and XP helper. Skill affects time, not yield or decay recovery.

Artificial-part recovery is separate: its surgical speed, qualifications, chance of success and Medicine XP do not use the extraction weighting. The 100% work-speed timing examples assume 5/5/5 skills, normal capacities and no other work-speed modifiers.

### Artificial-part recovery

Recovery identifies artificial parts and salvage items from hediff definitions; it does not list individual implants or species. Clothing and equipment are stripped first. Nested implants are processed before their enclosing artificial limbs. The operator must be capable of Medicine and satisfy the matching installation recipe's skill requirements (or the removal recipe when no installation recipe exists).

Each part uses the matching removal recipe's work and medical operation speed; generic vanilla part-removal work is the fallback. Medical work awards Medicine XP. Success uses the matching installation surgery's outcome-quality components, including surgeon ability, recipe difficulty, applicable inspirations and the vanilla probability cap. The Gene Vat supplies applicable environmental factors such as lighting and being outdoors, with the room cleanliness factor removed. Medicine-quality and patient-bed components are omitted. No medicine, anaesthetic or live-patient injury outcomes are used. Parts without an installation surgery use removal surgery factors.

Each completed attempt physically removes its hediff from the corpse before producing salvage. Failed recovery destroys the artificial part; successful recovery creates the item specified by `spawnThingOnRemoved`. Parts without a defined removal item are discarded. Removing a replacement limb leaves it missing instead of restoring an organic limb, preventing artificial material from becoming biomass. Completed removals therefore cannot be rerolled by interrupting or restarting the job. Interrupted unfinished removal can be attempted again, with no recovery roll until its work completes. Biomass extraction starts after all remaining artificial parts have been handled.

Manual checks: process a corpse with multiple implants; compare low/high qualified medics; confirm no medicine is consumed; compare dirty/clean rooms with identical lighting; interrupt after one removal and verify it does not reappear or produce twice; confirm extraction continues after successful and failed salvage. Mods that supply custom removal effects rather than a standard removal item may need compatibility support.

Corpse biomass is `max(1, floor(material amount × 0.5 × 1.25 × decay))`. Material amount uses vanilla MeatAmount with post-processing disabled: body size, missing natural parts, slaughter condition, nutrition and difficulty still apply, but vanilla's small-animal meat bonus does not. The whole-body multiplier includes the remaining organs, bones and hide even when limbs are missing. Decay multipliers are 1 for fresh, 0.5 for rotting and 0.1 for desiccated bodies. Every eligible corpse yields at least one biomass.

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

The view shows current fresh/rotting/desiccated timing percentages. These settings apply to newly started corpse extraction, not running jobs or meat/crop bills. Settings persist across saves. Refined biomass options are grouped separately below, with an independent reset button.

### Calculation unit tests

The .NET 10 xUnit project compiles the production `BiomassCalculations.cs` source directly, without loading RimWorld or running the deployment script. This tests the exact math used by the mod while keeping the test runner independent of a game installation. It covers yield and rounding, damaged bodies, minimum recovery, decay/handling timing, equal-yield comparisons, batch efficiency, operator speed and configurable minutes.

```powershell
dotnet test Source/GeneFoundry/GeneFoundry.Tests/GeneFoundry.Tests.csproj
```

The same test project also checks all mod definition XML for parsing and surrounding text whitespace, duplicate definition names, unresolved GeneFoundry references, recipe/product reference types and custom classes included in the production project. Regression checks cover Gene Vat recipe/workgiver registration and corpse ingredient safety/decay settings. Fixtures are copied from the real definitions and sources at build time; no game installation is needed for these checks.

The game mod remains .NET Framework 4.7.2. These static checks do not replace RimWorld's definition loading, inheritance and cross-references to vanilla or other mods. UI rendering, job behavior and game-object integration still require in-game testing.

Before v1, development prioritises current behavior over compatibility with older development saves. Normal save/load of current jobs and settings remains supported; migration of old development saves is not a release requirement.

## Biomass refinery

Each attempt consumes **10 raw biomass + 1 neutroamine** when preparation finishes. A successful batch produces **2 refined biomass**. Failed batches produce nothing and do not refund inputs.

- Build hoppers along any side of a completed refinery with their outlets facing it. Diagonal hoppers do not connect. Keep the interaction cell clear. Multiple hoppers combine their stored biomass without speeding production.
- Assign an operator to Research work and add a refinement bill. The operator collects neutroamine, prepares the batch and then leaves automated processing to the machine.
- Minimum skills are Intellectual 5, Crafting 3 and Medicine 2. Benefits cap at 10, 8 and 6 respectively. Their weights are 60%, 25% and 15%.
- At default settings, preparation scales linearly from 60 to 30 in-game minutes. Failure probability scales from 30% to 0%. The failure probability is captured when processing starts.
- Preparation awards 0.1 total base XP per tick, split across the three skills using the same weights. Vanilla passion and learning modifiers apply. Interrupted preparation retains earned XP; hauling and automated processing award none.
- Default processing takes 12 in-game hours. Each available gene processor removes 5% of the base duration; ten reach the six-hour minimum. Reductions add together rather than compound.
- Powered processors must satisfy vanilla facility range and line-of-sight checks. An assembler with an active job takes priority even between operator work sessions. Otherwise, each processor assists the nearest active, powered refinery that can still benefit; ties use building ID. Assistance updates every 60 game ticks.
- Changing assistance adjusts future processing speed while preserving completed progress. Extra processors beyond a refinery's useful limit remain available to other refineries.
- Idle power is 100 W; processing power is 1,500 W. Power loss pauses processing. A continuous 5,000-tick outage (two in-game hours) destroys the batch; restoring power resets the timer.
- **Do X times** counts attempts when processing starts. **Do until X** uses vanilla output-count rules and continues requesting batches until its target is reached.
- Successful completion rolls only once, including when output placement must be retried. Preparation duration, batch progress, failure probability, completion-roll state and outage time are saved.

### Settings and testing

Open **Options → Mod Settings → GeneFoundry** to change preparation durations (1–120 minutes), processing durations (5 minutes–24 hours) and processor reduction (0–25%). Minimum durations cannot exceed base durations. Processing changes affect running batches; preparation changes apply to new preparation jobs. Settings are shared across saves. Restore defaults after accelerated testing if desired.

The XML processing duration of 2,500 ticks is retained solely to migrate saves made before settings existed. Runtime processing uses Mod Settings. The developer start-refinement command bypasses operator preparation and starts a zero-risk batch.

### Final in-game regression checks

Use a disposable save and short processing settings:

1. Run two refineries near shared processors; confirm no processor assists both and excess capacity can assist the second refinery.
2. Start, interrupt and cancel an assembler job. Verify it retains priority during operator breaks and releases processors when its job ends.
3. Save/reload during preparation, assisted processing and a power outage. Check progress, learning, risk and outage time continue correctly.
4. Check Do X times and Do until X after both successful and failed batches.
5. Load an existing colony with a refinery built before processor support, and confirm nearby processors can assist it.

The next development focus is finishing the Gene Vat/raw biomass stage. Gene Fabricator implementation remains deferred.
