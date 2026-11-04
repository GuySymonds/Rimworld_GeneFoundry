# Research progression

[Back to overview](../README.md) · [Biomass production](Biomass.md) · [Gene fabrication](Fabrication.md)

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

## Gene fabrication research

Cosmetic categories unlock directly once **Gene fabrication** is complete, with no category research project or extra research work. This includes `Cosmetic` and the `Cosmetic_` category family, including modded categories with those stable definition names. Cosmetic archite genes still require vanilla **Archogenetics**. Older automatically completed cosmetic projects remain supported when present, but new cosmetic projects are not generated.

Gene category projects are generated during definition resolution from the loaded `GeneDef.displayCategory` values. This includes modded and generated genes, zero-complexity cosmetic genes and genes excluded from random gene-set generation. Empty categories do not produce projects; genes without a category share an Uncategorized project. Stable project names use category definition names, not translated labels. All category projects remain in the GeneFoundry tab. Their descriptions list their genes and explain their cost breakdown.

Default category research cost:

`clamp(ceil((500 + (100 × gene count) + (200 × total complexity) + (500 × total archite value)) × 25%), 1,000, 4,000)`

Each gene's complexity and archite value are clamped independently to at least zero before summing, so negative modded stats cannot cancel another gene's cost. Each gene definition counts once. Scale the full calculation, round up, then apply the category minimum and maximum. Defaults reduce the original costs to one quarter, bounded between 1,000 and 4,000: a Miscellaneous category in the original balance example falls from 6,800 to 1,700, Aptitudes from 19,700 to 4,000, and Archite from 12,200 to 3,050. Small categories cost at least 1,000. Categories at either limit cost the same despite different contents. Wide arithmetic prevents overflow.

The research view keeps the main progression above a compact four-column category grid. Category labels show only the category name. Category requirements use vanilla `hiddenPrerequisites`: they still prevent research and appear in the selected project's prerequisite details, but do not draw lines across the grid. Category projects remain independent; the layout introduces no extra research prerequisites. Stable definition names preserve research identity despite the new labels and positions.

**Archite genes require vanilla Archogenetics** (`Archogenetics`) as well as the foundation and their category project. An archite gene is identified by `biostatArc > 0`, regardless of category. Mixed categories can be researched without Archogenetics so their ordinary genes become available; their archite genes stay locked. Non-cosmetic archite-only categories also list Archogenetics as a research prerequisite. Cosmetic category completion is automatic, while archite gene fabrication remains gated individually. Setting the archite surcharge to zero does not remove this requirement. Vanilla gene extraction and assembly are unaffected.

The separate **Gene research** settings section provides sliders for the fabrication foundation (100–10,000; default 1,500), category base cost (0–5,000; default 500), cost per gene (0–1,000; default 100), cost per complexity point (0–2,000; default 200) and cost per archite value (0–5,000; default 500). Category cost percentage (1–200%; default 25%), minimum category cost (100–10,000; default 1,000) and maximum category cost (minimum–10,000; default 4,000) apply after these terms. Raising the minimum above the maximum raises the maximum too. These controls do not affect the fabrication foundation or Archogenetics requirements. Missing settings take the new defaults without resetting custom term weights. The section has an independent reset button. Restart RimWorld after changing these settings. Existing research progress remains in points, so increasing costs can make even a formerly completed project require more work. Earlier biomass research costs are unchanged.

The Fabricator uses the shared `GeneFabricationResearch.IsUnlocked(GeneDef)` permission check both when queuing an order and during work. Research completion alone does not create genepacks.

## Verification

See [research test coverage and runtime checks](Testing.md#research).
