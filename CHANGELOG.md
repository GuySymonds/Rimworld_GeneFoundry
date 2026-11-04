# Release notes

## Unreleased

- Halve runtime texture dimensions only in generated Steam release packages, preserving full-resolution development assets and deployments.

- Rebalance extraction around dry mass (meat 25%, plants 24%, whole corpses 2.2× meat dry mass), with 60% basic and 75% enzymatic recovery. Refine 45 raw biomass plus 1 neutroamine into 10 refined biomass; automated defaults are now 6 hours, reduced by 20 minutes per processor to a 1-hour minimum with 15 processors. Preserve saved duration settings, convert saved percentage assistance to equivalent minutes, and keep existing preparation/extraction timings.
- Allow eight rows per research category column and label the research tab “Gene Foundry”.

- Deploy Debug builds to `GeneFoundry_dev` and Release builds to `GeneFoundry`; allow first deployment into missing or empty folders while retaining existing-installation safety checks.

- Upgrade CodeQL to v4, document incomplete C# dependency resolution, and align main-branch protection with squash merging and required portable/CodeQL checks.

- Share contributor guidance between Codex and Copilot; replace the long README with links to focused gameplay, artwork, testing and release documents.

- Interrupt refinery preparation and stop XP when power/workstation usability, bill eligibility or worker drafting prevents work, without consuming batch inputs.
- Preserve completed refinery output through outages and save/load while placement is blocked. Retry without power or rerolling; use idle power and release processor assistance. Incomplete reactions retain two-hour outage spoilage.
- Add managed refinery lifecycle regressions for interrupted preparation after loading, stopped XP, completion/output retries, long outages and exact active-reaction spoilage timing.

- Raw biomass now spoils after six days at normal temperature, using vanilla refrigeration/freezing rules. Sealed biomass hoppers pause spoilage without power, retain prior rot progress and protect against environmental deterioration. Refined biomass remains shelf-stable and damageable.
- Add portable GitHub Actions checks and accurately scoped status/compatibility/license badges.
- Repair the managed game-test source list, document repository conventions in AGENTS.md and standardize refinery asset/deployment script names.
- Fix package validation for the custom directional Fabricator graphic and validate code-referenced refinery/Fabricator animation assets.

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
