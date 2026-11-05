# Release notes

## 1.1.0 — 5 November 2026

- Add the default-enabled **Enable AI Slop** graphics option. Disable it for static Simple artwork for the Gene Vat, Biomass Refinery, Gene Fabricator and Biomass Hopper, with four authored orientations each. Restart RimWorld after changing it.
- Retain complete Detailed artwork and animations for each direction whose optional Simple image is missing. Simple directions omit machine animations and decorative work effects; production behavior, draw sizes and interaction cells remain unchanged.
- Skip background corpse-presentation pawn scans when all four Vat directions use Simple graphics. Detailed fallback retains the existing presentation.
- Correct Simple Hopper east/west orientation and its description of the existing 45-biomass refinery requirement; gameplay quantities are unchanged.
- Standardise runtime texture names while preserving original PNG data, Detailed animation sequences and timing. Editable artwork and source screenshots/GIFs remain outside the runtime package.
- Declare RimWorld 1.7 support alongside 1.6; Biotech remains required. Preserve released-save identities, job progress, ownership and existing settings; old settings default to Detailed graphics.
- Deploy Debug and Release builds into the same GeneFoundry folder, using version metadata instead of a duplicate development copy.
- Track the public Workshop identity and include it in distributions. Release packaging checks valid, matching publication IDs before building or cleaning staging, preventing accidental updates to a different Workshop item.

## 1.0.0 — 4 November 2026

First public release for RimWorld 1.6 with Biotech.

- Three-stage production: Gene Vat extraction, hopper-fed Biomass Refinery, and manual Gene Fabricator. No gene samples required.
- Meat, raw plant food and organic-corpse extraction, optional herbal enzymes, corpse stripping and artificial-part recovery. Extraction uses dry mass (meat 25%, plants 24%, whole corpses 2.2× recoverable meat dry mass), with 60% basic or 75% enzymatic recovery.
- Weighted operator wastage with guaranteed raw recovery; skilled operators gain extraction speed. Rot-stink incidents apply to all extraction inputs.
- Raw biomass spoils after six days at normal temperature, using vanilla refrigeration/freezing rules. Sealed hoppers pause spoilage without power, retain prior rot progress and prevent environmental deterioration. Refined biomass is shelf-stable and damageable.
- Each successful refinery batch converts 45 raw biomass and 1 neutroamine into exactly 10 refined biomass. Preparation stays linear from 90 to 30 minutes over weighted skill 5–20; operator failure falls from 30% at 5 to 5% at 10, then 0% at 20. Dirt, exposure and dim light add risk.
- Automated refinement defaults to 6 hours, reduced by 20 minutes per processor to a 1-hour minimum with 15 processors. Saved duration settings are preserved; saved percentage assistance converts to equivalent minutes.
- Refinery preparation and XP stop when power, workstation usability, bill eligibility or worker drafting prevents work, without charging batch inputs. Incomplete reactions pause without power and spoil after a two-hour outage.
- Completed refinery output remains safe through outages and save/load while placement is blocked. Placement retries work without power or rerolling, use idle power and release processor assistance.
- Single- and multi-gene genepacks, queued bills, saved unfinished work, original-operator ownership and exactly-once ingredient/output handling. Multi-gene biomass pricing discounts genes by cost rank: 100%, 90%, 80%, 70%, 60%, then 50% each.
- Dedicated “Gene Foundry” research tab with up to eight rows per category column. Gene fabrication unlocks basic extraction/refinement; Enzymatic extraction follows it. Loaded genes generate category research, completed projects retain completion when mods or costs change, and archite genes retain the Archogenetics gate.
- Animated production artwork, persistent rot-stink incidents, configurable processing settings and shared environmental penalties.
- Late-game Fabricator construction investment; refinery uses 100 W idle / 1,000 W processing.
- Validated Workshop metadata and artwork, portable game assembly references, and one-command release packaging without deployment side effects. Generated Steam packages halve runtime texture dimensions while preserving full-resolution development assets and deployments.
- Debug builds deploy to `GeneFoundry_dev`; Release builds deploy to `GeneFoundry`. Missing or empty targets are supported, with identity and publication-ID safety checks for existing installations.
- Portable GitHub Actions checks, CodeQL v4, managed game regressions and release workflow safety checks. Contributor guidance is shared between Codex and Copilot; focused documentation covers gameplay, artwork, testing and release procedures.
