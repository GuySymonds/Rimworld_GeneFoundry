# Processing environment penalties

Settings are under **GeneFoundry → Processing environment — all three machines**.
Existing saved dirt/exposure settings are reused; darkness defaults to 10 points
when loading older settings. Each penalty slider ranges from 0–50 points; zero
disables that penalty. The cleanliness checkbox applies to dirt only.

| Condition | Default | Extraction | Refinement | Fabrication |
|---|---|---|---|---|
| Indoor dirt | 0 at cleanliness 0+, rising linearly to 15 points at −2 | None | Added failure risk | Added failure risk |
| Outdoor/unroofed room | 10 points, replacing dirt | Added wastage | Added failure risk | Added failure risk |
| Dim light | 10 points at darkness, tapering to 0 at 30% light | Added wastage | Added failure risk | Added failure risk |

Exposure follows the existing refinery definition: no room, an outdoor room,
a room touching the map edge, or a room with any missing roof. Lighting uses
the machine's operator interaction cell and includes daylight. Dirt and outdoor
exposure do not stack; light stacks with whichever applies. Sterile rooms do not
provide a bonus. These changes do not alter work speed or artificial-part recovery.

- **Extraction:** applies environment wastage at completion, alongside the existing
  single skill roll, for meat, plants and corpses, including enzymatic recipes.
  For example, an expert yielding 100 biomass gets 90 outdoors in good light,
  or 80 outdoors in darkness. Output remains at least one biomass.
- **Refinement:** captures the combined penalty when automated processing starts.
  Saved active batches retain their original risk. A new saved light-level field
  supports inspection; absent fields in older saves default to full light.
- **Fabrication:** accumulates combined failure risk weighted by actual work done,
  using the existing saved risk accumulator. Cleaning or adding lights affects
  remaining work only. Skill risk still tops out at 30%; total risk can reach 100%.
  Older unfinished work retains its saved contribution and remains valid.

Core calculations are in `Recipes/BiomassCalculations.cs`; live room and lighting
sampling is in `Recipes/ProcessingEnvironment.cs`. Machine inspection shows current
conditions; refinery inspection shows the captured conditions for its active batch.

Runtime checks: compare clean/dirty rooms, outside and a missing roof; switch lights
through 0%, 15%, 30% and full light at the interaction cell. Check all extraction
types, clean/change lights during fabrication, and save/reload each process. Refinery
risk should remain fixed after batch start; fabrication should retain earlier risk;
extraction should use completion conditions. Automated tests do not simulate lighting
or room classification in a running colony.
