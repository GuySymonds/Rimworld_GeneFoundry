# Rimworld_GeneFoundry
GeneFoundry — A RimWorld biotechnology mod that transforms organic matter into genetic resources for refining and fabricating custom genes.

For RimWorld 1.6 with Biotech. The Gene Vat and Biomass Refinery are implemented; the Gene Fabricator is planned.

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
