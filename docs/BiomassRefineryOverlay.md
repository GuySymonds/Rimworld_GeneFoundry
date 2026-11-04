# Biomass refinery vat overlay

The existing `CompBiomassRefiner.TryStartBatch` starts the visual clock only after
preparation and successful input consumption. The component saves `gfVatHasProcessed`
and `gfVatStartedAt` per refinery. Drawing reads these and the existing processing
flag/progress; it never advances the batch or changes its duration, resources or output.
Game ticks hold filling frames 00–07 once, then loop processing frames 00–15.
During a temporary power outage the visual clock pauses with processing, preserving
the current frame and partial-frame progress. Restoring power resumes from there.
The adjusted start tick is saved, so save/load during an outage retains that position.
The existing spoilage timer continues; spoilage displays the dirty vat.
Completion (including blocked output placement) and failed batches also display dirty.
Gameplay completion/output rules are documented under [Biomass refinery](Biomass.md#biomass-refinery).
Preparation interruptions and deleting/suspending preparation bills do not start filling.
There is currently no separate active-batch cancellation action; any end path clearing
the existing processing flag automatically leaves a used vat dirty.

## Editing alignment

Edit `VatOverlayTransforms` near the top of
`Source/GeneFoundry/GeneFoundry/Components/BiomassRefineryOverlay.cs`.
Its four entries are **North, East, South, West**, matching `Rot4.AsInt`.
Each entry contains **TopLeft, TopRight, BottomLeft, BottomRight** in that order.

Each `Vector2(x, z)` is a destination corner in map-tile units, relative to
the building's drawing origin. Positive X moves right; positive Z moves up the map.
The initial coordinates are approximate fits to the supplied directional chassis.

- Position: add the same X/Z offset to all four corners.
- Horizontal scale: spread or contract the left/right corners around their centre X.
- Vertical scale: spread or contract the top/bottom corners around their centre Z.
- Warp: adjust individual corners to form the desired trapezoid/skew. Keep the
  quad convex and preserve corner ordering to avoid folds.

The full source canvas maps UV top-left to destination top-left, etc. Two triangles
interpolate the texture; this is an affine quad, not a projective shader. No automatic
rotation or mirroring is added. All 26 textures use the same mesh for a direction.
No per-frame position configuration exists. Meshes and transparent materials are
cached across refineries, with no normal draw-time allocations.

The chassis graphic is unchanged. `RealtimeOnly` in the refinery definition ensures
the existing building draw lifecycle invokes the component overlay every draw.
The overlay sits 0.002 tiles above the chassis, below ordinary item/pawn layers.

## Timing and assets

Edit `FillingTicksPerFrame` and `ProcessingTicksPerFrame` in
`Source/GeneFoundry/GeneFoundry/Components/BiomassRefineryAnimation.cs`.
Filling defaults to **10 game ticks per frame**, processing to **5**; use positive integers.
Filling lasts 80 ticks; the mixing loop lasts 80 ticks. Pause freezes the frame and game speed scales
playback. Each building has its own start tick; redraws/inspection do not reset it.
Rebuild and restart RimWorld after changing code settings or corner coordinates.

Textures are loaded from
`Mod/Textures/GeneFoundry/Buildings/BiomassRefineryFrames/`.
The 26 vat-overlay PNGs share one RGBA canvas. The same folder also contains 32 directional preparation frames (`prep00_north.png` through `prep07_west.png`), drawn only during active manual preparation. Missing assets log once per texture
and omit that overlay frame, retaining the underlying chassis.

Old active saves without a timestamp resume at the mixing phase. Old idle saves
have no historical usage information and initially appear clean; subsequent usage
is persisted normally. New saves preserve clean/dirty, filling and mixing state.

## Verification

Automated coverage checks filling and loop boundaries, held frames, independent
elapsed times, RGBA asset dimensions/count and actual Scribe round-trips for clean,
dirty, filling, mixing and completed-pending states. These checks do not render Unity.

In RimWorld, verify alignment/alpha in all four directions, the clean/preparation/
filling/mixing/dirty transitions, pause and speed changes, two staggered refineries,
power restoration/spoilage, completion/failure and save/load during filling/mixing.
Fine-tune the four quads against the chassis at normal play zoom.
