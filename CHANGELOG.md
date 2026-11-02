# Release notes

## 1.0.0

First public release for RimWorld 1.6 with Biotech.

- Three-stage production: Gene Vat extraction, hopper-fed Biomass Refinery, and manual Gene Fabricator.
- Meat, plant and organic-corpse extraction, herbal catalysts, corpse stripping and artificial-part recovery.
- Weighted operator wastage with guaranteed raw recovery; skilled operators gain extraction speed. Rot-stink incidents apply to all extraction inputs.
- Fixed refinery conversion: 40 raw biomass plus 1 neutroamine produces 10 refined biomass on success.
- Refinery preparation stays linear from 90 to 30 minutes over weighted skill 5-20. Operator failure uses piecewise interpolation: 30% at 5, 5% at 10, 0% at 20. Dirt and exposure add risk.
- Automated refinement defaults to 36 hours, with processor assistance down to 18 hours, power-loss pausing and two-hour outage spoilage.
- Single- and multi-gene genepacks, saved unfinished work, dedicated gene-category research, and Biotech archite integration.
- Animated Gene Vat artwork and persistent rot-stink incidents.
- Late-game Fabricator construction investment; refinery uses 100 W idle / 1,000 W processing.
- Validated Workshop metadata and artwork, portable game assembly references, and one-command release packaging without deployment side effects.
