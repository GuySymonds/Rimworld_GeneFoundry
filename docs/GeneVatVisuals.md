# Gene Vat artwork and animation

[Back to overview](../README.md) · [Extraction behavior](Biomass.md) · [Refinery artwork](BiomassRefineryOverlay.md)

## Graphics styles and texture names

In **Options > Mod Settings > GeneFoundry > Graphics**, **Enable AI Slop** selects Detailed artwork and animations when enabled (the default for new installations and released settings without this key). Disable it for available Simple static sprites. **Restart RimWorld after changing the setting**; the running session keeps its original selection so cached materials cannot mix styles.

Selection is independent for every machine and direction: a missing optional Simple image quietly uses that direction's Detailed base, animations and overlays. Present Simple images suppress the machine's animation layers, decorative bubbles and vanilla recipe visual work effects; work sounds remain unchanged. Simple vats use ordinary RimWorld corpse/ingredient drawing at their existing positions, without custom basin fitting or staged corpse-decay visuals. Actual rot-stink gas, processing, unfinished items, saved progress and ingredient charging remain unchanged. Detailed rendering retains its existing shaders, meshes, dimensions, offsets and timing.

All paths below are relative to `Mod/Textures/GeneFoundry/`. Names are case-sensitive: use PascalCase asset/style/state components separated by underscores, two-digit frame numbers, lowercase `north`, `east`, `south`, `west`, and lowercase `.png`. Omit inapplicable components. Non-directional overlays retain their original geometry rather than gaining artificial directional frames.

| Folder | Filename pattern | Frames |
| --- | --- | --- |
| `Buildings/` | `<machine>_Detailed_<direction>.png` | Four bases per machine |
| `Buildings/` | `<machine>_Simple_<direction>.png` | Optional, one per direction |
| `Buildings/GeneVatFrames/` | `GeneVat_Detailed_Transition_<frame>_<direction>.png` | 00–07 per direction |
| `Buildings/GeneFabricatorFrames/` | `GeneFabricator_Detailed_Idle_<direction>.png` | Four overlays |
| `Buildings/GeneFabricatorFrames/` | `GeneFabricator_Detailed_Processing_<frame>_<direction>.png` | 00–07 per direction |
| `Buildings/BiomassRefineryFrames/` | `BiomassRefinery_Detailed_Preparation_<frame>_<direction>.png` | 00–07 per direction |
| `Buildings/BiomassRefineryFrames/` | `BiomassRefinery_Detailed_Clean.png`, `BiomassRefinery_Detailed_Dirty.png` | Static vat states |
| `Buildings/BiomassRefineryFrames/` | `BiomassRefinery_Detailed_Filling_<frame>.png` | 00–07 |
| `Buildings/BiomassRefineryFrames/` | `BiomassRefinery_Detailed_Processing_<frame>.png` | 00–15 |
| `Effects/BubbleMass/` | `BubbleMass_<frame>.png` | 00–07 |
| `RawBiomass/`, `RefinedBiomass/` | `RawBiomass_A.png` / `RefinedBiomass_A.png` (also `_B`, `_C`) | Stack variants |
| `Buildings/` | `BiomassHopper_Detailed_<direction>.png`, `BiomassHopper_Simple_<direction>.png` | Four bases per style |
| Root | `UnfinishedGenePack.png` | No style variants |

Simple Vat, Refinery, Fabricator and Hopper artwork is supplied in all four directions. Each uses the same toggle and retains per-direction Detailed fallback if an optional Simple image is removed.

To add Simple artwork, export a complete transparent directional PNG into `Buildings/` using `GeneVat`, `BiomassRefinery`, `BiomassHopper` or `GeneFabricator` as `<machine>`. Do not create placeholders for missing directions. Restart after adding images; no C# changes are needed. Each authored direction is used directly without extra rotation or mirroring. All styles use the existing draw sizes: Vat `(3,3.6)`, Refinery `(3.2,2.2)`, Fabricator `(5.4,2.4)`, Hopper `(1.2,1.2)`; east/west use the native horizontal mesh dimensions. Compose transparent margins for those canvases. Footprints, interaction cells, altitude and draw offsets are unchanged. The supplied square Simple Vat canvases deliberately use the existing Vat draw size. Source artwork under `Source/Textures/` retains its artist filenames.

The graphics setting adds only the `useDetailedGraphics` settings key (default `true`). Released setting keys, definition IDs, package ID and persisted types are preserved; no save migration is required. Texture paths were renamed; external retexture path compatibility is not provided.

## Corpse liquidation visuals

During actual corpse extraction, the corpse visually advances through fresh, rotting and desiccated artwork at one-third and two-thirds progress. An already decayed corpse never looks fresher. Actual rot, yield, work, placement and ingredient handling are unchanged.

Presentation measures the native render meshes with their laying orientation, node and life-stage offsets. Small corpses keep their natural scale; oversized corpses shrink proportionally using twice the basin width and height as their mesh fit allowance; the natural-size cap remains. This relaxed allowance compensates for sprite padding and can let large silhouettes extend beyond the basin. The pose and one fit scale are cached for the work session across all three rot appearances. Each appearance is centred on the same basin centre. Mesh bounds are conservative and include transparent texture padding; no biological body-size guesses or GPU texture readbacks are used. The corpse's map position is unchanged.

Eight final BubbleMass textures live in `Mod/Textures/GeneFoundry/Effects/BubbleMass` and are copied unchanged during deployment. The overlay uses alpha blending, preserves the 500:203 aspect ratio and sits 0.06 tiles inside the basin edge nearest the operator in every orientation. It loops forward 00 through 07 at 6 game ticks per frame (48 ticks per cycle), above the corpse and below ordinary item/pawn layers. Each work session has its own tick origin. Pausing freezes playback. Hauling, stripping and part recovery do not show the overlay. Active meat and crop extraction also show the bubbles, using the same cached resources. Interruptions and power loss remove it; normal corpse drawing returns. Save/load rebuilds this transient presentation from the active job.

The native Gene Vat graphic uses `BuildingOnTop`, above vanilla walls' `Building` layer, for both idle and animated materials. Its footprint, draw size, interaction cell and directional images are unchanged. The vial animation keeps its existing sequence and timing.

Manual checks still required in RimWorld: process rats, hares, humans, muffalo, thrumbo and an oversized modded animal in different laying orientations, including already decayed bodies. Check centring and constant size through both rot transitions, alpha blending and containment of bubbles, exposed metal edges and the underlying vial animation. Interrupt/draft, cut power, finish work and save/reload; confirm no doubled or missing corpse. Run two vats with staggered start times. Compare idle and active vats against a northern wall and away from walls, checking nearby pawn/item overlap. Automated tests cover fit calculations, transformed mesh bounds and centring, animation timing, independent presentation lifecycle, asset identity and definition/layer wiring; they do not verify Unity rendering.

## Gene Vat machine animation

The rotatable vat selects directional idle artwork through its custom Graphic_Multi subclass until a pawn reaches actual biomass extraction. Meat, plant and corpse extraction use eight aligned runtime frames per direction, held for 6 game ticks each, in the sequence 00–07–01 (14 displayed frames, 84 ticks). Pausing freezes the frame; game speed controls playback. Each vat starts its own visual cycle. Hauling and artificial-part recovery remain static. The base selects its directional sprite without rotating that artwork; the transition layer selects the matching directional frame and draws it on the same mesh without an extra rotation. Its bubbles remain behind the animal, with the separate foreground BubbleMass layer near the operator. Basin contents and foreground bubbles use measured bounds for each view and follow its processing orientation. Explicit material queues order transitions, corpse/ingredients and foreground BubbleMass in that order. Missing animation assets log once and fall back to the static sprite. No animation state is saved.

In-game checks still required: exercise all six extraction bills, check idle/hauling/part recovery, draft or interrupt the worker, cut power, finish a bill, and save/reload during work. Check two vats with different start times. Confirm alpha edges, alignment, dimensions, corpse placement/liquidation and selection/placement previews remain correct. Automated tests verify timing, sequence, definition wiring and runtime PNG dimensions, transparency formats and frame counts; they do not verify Unity rendering.

Material extraction draws delivered meat/crop stacks inside the lower basin beneath the bubbles, shifted 0.25 cells towards the top so more of the ingredients remain visible. Herbal medicine is hidden during both material and corpse processing, without changing its consumption or position. Ingredient graphics return on interruption; map positions and tracked quantities are unchanged. Biomass extraction work is halved for meat, crops and corpses; artificial-part recovery and animation cadence are unchanged.

Final Gene Vat artwork belongs in `Mod/Textures/GeneFoundry/Buildings/`: provide `GeneVat_Detailed_north.png`, `GeneVat_Detailed_east.png`, `GeneVat_Detailed_south.png`, and `GeneVat_Detailed_west.png`. Keep the XML graphic class as `GeneFoundry.Recipes.Graphic_GeneVat` to retain animation; it inherits `Graphic_Multi`. Export eight frames per direction into `GeneVatFrames/`, named `GeneVat_Detailed_Transition_00_north.png` through `GeneVat_Detailed_Transition_07_north.png`, and likewise `_east`, `_south`, and `_west`. Each frame must use the exact canvas dimensions and alignment of its directional base (currently 1086x1448 north/south and 1448x1086 east/west). Directional frames are not rotated again. A missing or incomplete set leaves that direction static and logs an error; release validation requires all 32 frames. Basin bounds for job contents and foreground bubbles are measured per view in `MapComponent_CorpseLiquidationVisuals.BasinBounds`. Keep working files in `Source/Textures/`; nothing copies from that directory automatically.
