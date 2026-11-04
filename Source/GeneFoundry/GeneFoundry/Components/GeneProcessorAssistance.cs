using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GeneFoundry.Components
{
    public class GeneProcessorAssistance : MapComponent
    {
        private readonly Dictionary<Thing, int> counts = new Dictionary<Thing, int>();
        private int lastUpdate = -1;

        public GeneProcessorAssistance(Map map) : base(map) { }

        public int ProcessorCount(Thing refinery)
        {
            int now = Find.TickManager.TicksGame;
            if (lastUpdate < 0 || now - lastUpdate >= 60)
            {
                Refresh();
                lastUpdate = now;
            }
            return counts.TryGetValue(refinery, out int count) ? count : 0;
        }

        private void Refresh()
        {
            counts.Clear();
            ThingDef refineryDef = DefDatabase<ThingDef>.GetNamedSilentFail("GF_BiomassRefinery");
            ThingDef processorDef = DefDatabase<ThingDef>.GetNamedSilentFail("GeneProcessor");
            if (refineryDef == null || processorDef == null)
                return;

            GeneFoundrySettings settings = GeneFoundryMod.Settings;
            int usefulProcessors = Recipes.BiomassCalculations.RefinementUsefulProcessors(
                settings.processingMinutes, settings.minimumProcessingMinutes, settings.processorReductionMinutes);

            List<Thing> refineries = map.listerThings.ThingsOfDef(refineryDef);
            List<Thing> assemblers = map.listerThings.ThingsOfDef(ThingDefOf.GeneAssembler);
            foreach (Thing processor in map.listerThings.ThingsOfDef(processorDef))
            {
                CompFacility facility = processor.TryGetComp<CompFacility>();
                if (facility == null || !facility.CanBeActive)
                    continue;

                // An assembler with an active job retains priority, even between work sessions.
                bool reserved = false;
                foreach (Thing thing in assemblers)
                {
                    var assembler = thing as Building_GeneAssembler;
                    if (assembler != null && assembler.Working &&
                        assembler.ConnectedFacilities.Contains(processor))
                    {
                        reserved = true;
                        break;
                    }
                }
                if (reserved)
                    continue;

                Thing recipient = null;
                float nearest = float.MaxValue;
                foreach (Thing refinery in refineries)
                {
                    counts.TryGetValue(refinery, out int allocated);
                    if (refinery.Faction != processor.Faction ||
                        allocated >= usefulProcessors ||
                        !refinery.TryGetComp<CompBiomassRefiner>().AcceptsProcessorAssistance ||
                        !CompAffectedByFacilities.CanPotentiallyLinkTo_Static(
                            processor, refinery.def, refinery.Position, refinery.Rotation, map))
                        continue;

                    float distance = (refinery.TrueCenter() - processor.TrueCenter()).sqrMagnitude;
                    if (distance < nearest ||
                        (distance == nearest && (recipient == null || refinery.thingIDNumber < recipient.thingIDNumber)))
                    {
                        recipient = refinery;
                        nearest = distance;
                    }
                }
                if (recipient != null)
                {
                    counts.TryGetValue(recipient, out int count);
                    counts[recipient] = count + 1;
                }
            }
        }
    }
}
