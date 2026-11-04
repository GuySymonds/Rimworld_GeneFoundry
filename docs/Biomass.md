# Biomass production and storage

[Back to overview](../README.md) · [Research unlocks](Research.md) · [Environment penalties](ProcessingEnvironment.md)

## Building costs and power

| Building | Construction materials | Power while switched on |
|---|---|---:|
| Gene Vat | 150 steel, 4 components | 250 W |
| Raw biomass hopper | 30 steel | None |
| Biomass Refinery | 200 steel, 6 components | 100 W idle / 1,000 W processing |
| Gene Fabricator | 250 steel, 8 components, 50 plasteel, 2 advanced components | 500 W |

The Fabricator is the late-game construction investment; its genepack ingredient costs and manufacturing rules are unchanged. Passive hoppers require no components. Vat and Fabricator power draw is constant while switched on, including idle time; the refinery switches between its existing idle and processing loads. Flick unused machines off to save power.

An unassisted 36-hour refinery cycle uses 36 kWh and produces 10 refined biomass on success. This excludes idle draw, processor power and failed batches. At the default 18-hour minimum, the refinery itself uses 18 kWh; assisting processors draw their own power. Power-loss pausing and spoilage after two continuous hours are unchanged.

## Storage and degradation

Raw biomass spoils after six days at normal temperature using vanilla rot rules. Refrigeration slows rot and freezing stops it. A biomass hopper provides sealed, refrigerated storage: spoilage pauses while biomass occupies its cell, without power or room cooling. This works even when disconnected from a refinery. Removing biomass resumes its existing rot progress; the hopper never makes old biomass fresh. Hoppers also protect stored contents from environmental deterioration. Outside protected storage, raw biomass retains its exposure deterioration rate of 1 and 75 hit points.

Refined biomass does not rot and has zero environmental deterioration. It can still suffer damage, fire or destruction. Ingredients in an incomplete refinery reaction follow the separate two-hour continuous-power-outage spoilage rule, rather than the loose biomass rot timer. Successful completed batches awaiting output space do not spoil. Existing raw stacks begin using the new rot component when loaded; no historical spoilage can be reconstructed.

## Extraction bills

The Gene Vat supports these material extraction bills:

| Input | Base raw biomass output (before operator wastage) |
|---|---:|
| 50 raw meat | 20 |
| 50 raw meat + 1 herbal medicine | 25 |
| 50 raw plant food | 8 |
| 50 raw plant food + 1 herbal medicine | 10 |

Each material recipe requires 500 work units, uses Cooking work assignment and allows mixing eligible ingredient types. Processing uses the weighted biomass extraction speed described below. The plant bill uses the vanilla `PlantFoodRaw` category, including modded foods assigned to that category. Use its ingredient filter to protect food reserves. It does not accept meals, meat or arbitrary non-food crops.

Basic and enzymatic corpse extraction bills use a custom job selected through vanilla bill and ingredient selection. They default to animal corpses; other organic corpses require explicit ingredient-filter changes. Both corpse recipes build an explicit allow list of corpse definitions belonging to loaded races with organic flesh. Vanilla mechanoid and drone flesh types remain ineligible even if marked organic. Ingredient and fixed bill filters use this allow list, so unknown mechanical corpse categories are excluded without naming each mod. Runtime processing uses the same eligibility rule. Enzymatic bills separately retain herbal medicine as a required ingredient; new bills still default to animal corpses. Installed artificial parts are removed medically before biomass extraction.

## Enzymatic crops and corpses

Enzymatic crops consume 50 plant food and 1 herbal medicine for up to 10 biomass, versus 8 without enzymes. Corpses recover 40% basic or 50% enzymatic from material multiplied by 1.10. Each enzymatic corpse costs `max(1, ceil(material / 50))` herbal medicine, using the original material before uplift or decay. Qualification requirements are unchanged.

All decay stages remain eligible. Fresh/rotting/desiccated multipliers are 100%/50%/10%. For 28 material, enzymatic base outputs are 15/7/1; for 560 material, 308/154/30. Wastage applies afterward. Enzymes do not affect salvage.

For corpse bills the workgiver locates the selected body through vanilla ingredient selection, calculates the full catalyst cost, and only schedules work when enough permitted, reachable, reservable herbal medicine exists within the bill's search radius. Vanilla bill collection toils deliver ingredients, including multiple medicine stacks. Medical salvage occurs first. The delivered catalyst is consumed at extraction completion, after checking that the full delivered quantity still exists; no partial catalyst charge is made when the full cost is missing. Interrupted extraction leaves unconsumed catalyst available, while completed medical removals remain completed.

Corpse recipe output is genuinely variable. Their labels say so, and no fixed `products` quantity is declared: this removes the misleading "Products: 1" add-bill tooltip. A dedicated recipe counter keeps Do until X available and counts actual raw biomass in the map and carried by pawns, respecting bill counting filters. The enzyme ingredient tooltip shows the minimum one-medicine requirement; the recipe description explains the scaled cost. Actual cost depends on the selected body, so it cannot be expressed as one fixed recipe quantity.

## Gene Vat skills

Basic meat and plant extraction require Cooking 3 only. All enzymatic and corpse extraction bills require Cooking 3, Medicine 3 and Intellectual 2. Vanilla bill selection enforces these recipe requirements. Artificial-part recovery additionally enforces the relevant surgery's requirements, which may be higher. This lets basic processing train an operator toward the advanced recipes.

All six extraction bills use Cooking 40%, Medicine 40% and Intellectual 20%, with reference levels 10/6/4. These are recovery references, not minimum qualifications. Each skill below reference contributes `weight * max(0, reference - level) / reference` to shortfall S. One roll per completed extraction chooses loss uniformly between 0.5S and S; output is `max(1, floor(baseOutput * (1 - loss)))`. Above-reference skills cannot compensate for another skill's shortfall. Cooking 5 / Medicine 6 / Intellectual 4 gives 10-20% loss: a 25-unit batch yields 20-22.

Speed stays at 100% when all skills are at or below reference. Above reference, each skill independently progresses from its reference to 20 using the same weights: `1 + weightedProgress * (maximumSpeed - 1)`. Default maximum is 200%. General work speed, sight and manipulation still apply. No skill grants extra yield. Vanilla material bill completion uses a cached loss result while retaining normal ingredient consumption and output handling.

Active extraction awards 0.1 total base XP per tick, split using the same weights. Vanilla passion and learning modifiers apply independently, so final earned XP need not retain the exact base proportions. No extraction XP is awarded during hauling. Meat and crop jobs retain vanilla ingredient collection, interruption, bill completion and output handling, with only the work phase extended to award weighted XP. Vanilla single-skill recipe XP is disabled to avoid awarding Cooking twice. Corpse work uses the same speed stat and XP helper.

Artificial-part recovery is separate: its surgical speed, qualifications, chance of success and Medicine XP do not use the extraction weighting. The 100% work-speed timing examples assume reference 10/6/4 skills, normal capacities and no other work-speed modifiers.

## Artificial-part recovery

Recovery identifies artificial parts and salvage items from hediff definitions; it does not list individual implants or species. Clothing and equipment are stripped first. Nested implants are processed before their enclosing artificial limbs. The operator must be capable of Medicine and satisfy the matching installation recipe's skill requirements (or the removal recipe when no installation recipe exists).

Each part uses the matching removal recipe's work and medical operation speed; generic vanilla part-removal work is the fallback. Medical work awards Medicine XP. Success uses the matching installation surgery's outcome-quality components, including surgeon ability, recipe difficulty, applicable inspirations and the vanilla probability cap. The Gene Vat supplies applicable environmental factors such as lighting and being outdoors, with the room cleanliness factor removed. Medicine-quality and patient-bed components are omitted. No medicine, anaesthetic or live-patient injury outcomes are used. Parts without an installation surgery use removal surgery factors.

Each completed attempt physically removes its hediff from the corpse before producing salvage. Failed recovery destroys the artificial part; successful recovery creates the item specified by `spawnThingOnRemoved`. Parts without a defined removal item are discarded. Removing a replacement limb leaves it missing instead of restoring an organic limb, preventing artificial material from becoming biomass. Completed removals therefore cannot be rerolled by interrupting or restarting the job. Interrupted unfinished removal can be attempted again, with no recovery roll until its work completes. Biomass extraction starts after all remaining artificial parts have been handled.

Corpse base biomass is `max(1, floor(material * 1.10 * efficiency * decay))`, using 0.40 basic or 0.50 enzymatic efficiency. Existing MeatAmount body-size, missing-part, slaughter, nutrition and difficulty adjustments remain, without the small-animal post-processing bonus. Operator wastage applies to this base output. 100 material yields 44 basic or 55 enzymatic before wastage.

Fresh processing time starts at `(setup minutes + material / 50 * extraction minutes) / 2`, divided by the operator's weighted biomass extraction speed. Defaults of 2.4 setup minutes and 24 extraction minutes per 50 material give 1.2 setup minutes and 12 minutes per 50 material before other modifiers. Above 50 material, multiply work by `max(0.75, (material / 50)^-0.1)`: larger bodies gain a gradual batch-efficiency benefit, capped at 25% less work.

Decay then multiplies processing time by `handling share + (1 - handling share) × decay multiplier`. With the default 30% handling share, fresh bodies take 100%, rotting bodies 65% and desiccated bodies 37% of fresh time. This makes a large desiccated body slower to process than a small fresh body yielding the same biomass. Time is captured after delivery/stripping, using the settings, operator speed and corpse condition at that point, and saved per job. Yield uses the condition at completion; a body that decays during work can therefore give less output without shortening a job already underway.

Clothing, equipment and inventory are stripped before extraction. Vanilla pawn butcher-product generation recovers special products, while meat and leather are consumed rather than produced separately. Existing meat and plant bills still use vanilla DoBill jobs through the Gene Vat workgiver; the old vanilla Cooking workgiver patch is replaced to prevent it bypassing the custom corpse job.

## Raw biomass timing options

Open **Options → Mod Settings → GeneFoundry → Raw biomass**. Sliders have hover explanations, and this section has its own reset button:

| Setting | Default | Range | Effect |
|---|---:|---:|---|
| Fresh extraction per 50 material | 24 minutes | 1–120 minutes | Halved to 12 minutes before other modifiers; scales with body size. |
| Setup time per corpse | 2.4 minutes | 0–15 minutes, 0.1-minute steps | Halved to 1.2 minutes before other modifiers; adds per-body work. |
| Work unaffected by decay | 30% | 0–100%, 1% steps | At 0%, time scales entirely with recovery. At 100%, decay does not shorten work. |
| Maximum extraction speed at level 20 | 200% | 150–300%, 1% steps | Skill-speed ceiling at all skills 20; progress starts at each reference. |

The view shows current fresh/rotting/desiccated timing percentages. Corpse duration is captured when extraction starts, while meat/crop speed responds during work. Operator wastage applies at completion to all extraction bills. The corpse-specific timing sliders do not affect meat/crop bills. Settings persist across saves. Refined biomass options are grouped separately below, with an independent reset button.

## Ingredient consumption and corpse placement

Vanilla hauling only attaches its ingredient-placement tracking callback to a fixed set of vanilla JobDefs. GeneFoundry's custom material and corpse jobs explicitly provide that same public callback when dropping their bill ingredients. This prevents fixed-output material recipes from creating biomass with an empty ingredient list and leaving inputs reusable. Material completion additionally validates that the full recorded recipe cost is still physically present before invoking vanilla recipe completion and consumption. Enzymatic corpse medicine is likewise consumed from tracked, delivered quantities at completion.

Corpses are placed at the front-centre tile **inside** the Gene Vat footprint, one tile behind its operator's interaction cell:

```text
x x x
x x x
x y x
```

Both corpse bills use this cell during medical recovery and extraction. A corpse already delivered elsewhere among the vat's ingredient cells is positioned there before work; another corpse occupying that spot blocks the job. Moving the processing corpse away interrupts work instead of processing it remotely.

## Gene Vat rot stink

During active meat, plant or corpse extraction (normal or enzymatic, at every decay stage), the current operator has a 250-tick incident check: Cooking, Medicine and Intellectual each add 5% risk when below level 10 (0/5/10/15% total). Hauling, idle time, stripping and implant recovery do not make checks. Partial check progress is retained on the vat across work sessions; active incidents suspend checks.

Each vat persists its own incident deadline and emission schedule. An incident lasts 2,500 game ticks despite interruptions, operator replacement or power loss. It emits 50 vanilla rot stink gas units at the corpse cell immediately and every 250 ticks thereafter (10 releases, 500 total). Despawned/destroyed vats emit nothing; despawned time still counts toward expiry. Existing gas disperses naturally. Vanilla controls visuals, diffusion, dissipation, exposure and lung rot; natural corpse gas is unchanged. The inspection warning appears only while an incident is active.

## Biomass refinery

Each attempt consumes **40 raw biomass + 1 neutroamine** when preparation finishes. Success produces exactly **10 refined biomass**; failure produces zero with no refund.

Hoppers must touch a side of the refinery and face it. Multiple hoppers combine their stock; ingredient validation, bill counting and hauling remain unchanged. Qualifications remain Intellectual 5, Crafting 3 and Medicine 2. The single weighted skill is `clamp(0.60 * Intellectual + 0.25 * Crafting + 0.15 * Medicine, 5, 20)`.

| Weighted skill | Preparation | Operator failure |
|---|---:|---:|
| 5 | 90 minutes | 30% |
| 10 | 70 minutes | 5% |
| 15 | 50 minutes | 2.5% |
| 20 | 30 minutes | 0% |

Preparation is `90 - 4 * (skill - 5)` minutes; operator failure is piecewise linear: `0.30 - 0.05 * (skill - 5)` from skill 5 to 10, then `0.05 * (20 - skill) / 10` from 10 to 20. No expert yield or preparation bonuses apply. Preparation awards existing weighted XP. Automated processing is independent: **36 hours**, **5% reduction per available gene processor**, **18-hour minimum** by default. Existing processor allocation gives active gene assemblers priority. Power loss pauses an incomplete reaction and spoils it after two continuous hours. Do X times counts initiated batches; Do until X targets stored output. Completion rolls once and retains the result across save/load and placement retries.

Preparation interrupts when the workstation becomes unusable (including power loss), the bill is suspended/deleted or no longer eligible, or the worker is drafted. No further preparation XP is awarded and no batch ingredients are consumed on interruption. Current-job save/load retains preparation duration and remaining work; it rechecks usability and bill state when toils resume. A cancelled job starts a fresh preparation when scheduled again.

A successful completed batch awaiting output space is shelf-stable, remains saved and blocks new batches until its output is placed. It retries placement even without electricity, uses the refinery's 100 W idle load when powered, and needs no processor assistance. Inspection shows **Output waiting for space**. Outages cannot spoil completed output or cause another failure roll; exactly 10 refined biomass is placed once space becomes available.

See [processing environment penalties](ProcessingEnvironment.md) for captured dirt, exposure and light risk, and [refinery artwork](BiomassRefineryOverlay.md) for overlay alignment and animation.

## Refinery settings

Open **Options -> Mod Settings -> GeneFoundry** for automated processing durations (5 minutes to 72 hours), processor assistance, raw extraction maximum speed and corpse timings. Corpse timing parameters are halved in the existing job; their stored defaults remain 24 and 2.4 minutes. Preparation follows the fixed linear formula. Reset buttons restore the new defaults; existing saved useful settings remain configurable. Expert yield, expert preparation and sterile-room bonus controls are removed.

The XML processing duration of 2,500 ticks is retained solely to migrate saves made before settings existed. Runtime processing uses Mod Settings. The developer start-refinement command bypasses operator preparation and operator/dirt risk; exposure and dim-light penalties still apply.

## Verification

See [test commands and coverage](Testing.md), [storage and extraction checks](Testing.md#biomass-storage-and-extraction), and [refinery runtime checks](Testing.md#refinery).
