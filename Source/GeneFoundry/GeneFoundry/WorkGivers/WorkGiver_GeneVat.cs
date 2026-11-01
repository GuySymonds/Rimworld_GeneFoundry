using RimWorld;
using Verse;
using Verse.AI;
using GeneFoundry.Recipes;
using System.Collections.Generic;
using System.Linq;

namespace GeneFoundry.WorkGivers
{
    public class WorkGiver_GeneVat : WorkGiver_DoBill
    {
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            Job vanillaJob = base.JobOnThing(pawn, thing, forced);
            if (vanillaJob == null || vanillaJob.def != JobDefOf.DoBill)
                return vanillaJob;

            string recipe = vanillaJob.bill?.recipe?.defName;
            bool enzymaticCorpse = recipe == "GF_ExtractBiomassCorpseEnzymatic";
            if (recipe != "GF_ExtractBiomassCorpse" && !enzymaticCorpse)
            {
                if (vanillaJob.bill?.recipe?.workSpeedStat?.defName == "GF_BiomassExtractionSpeed")
                    vanillaJob.def = DefDatabase<JobDef>.GetNamed("GF_ExtractMaterialBiomass");
                return vanillaJob;
            }

            if (vanillaJob.targetQueueB == null)
                return null;
            Corpse corpse = vanillaJob.targetQueueB.Select(target => target.Thing).OfType<Corpse>().SingleOrDefault();
            if (!CorpseBiomassUtility.CanProcess(corpse) ||
                !ArtificialPartRecovery.CanRecoverAll(pawn, corpse.InnerPawn))
                return null;

            if (enzymaticCorpse)
            {
                int remaining = BiomassCalculations.EnzymaticCatalystCount(CorpseBiomassUtility.RecoverableMeat(corpse));
                var targets = new List<LocalTargetInfo> { corpse };
                var counts = new List<int> { 1 };
                foreach (Thing herb in pawn.Map.listerThings.ThingsOfDef(ThingDefOf.MedicineHerbal)
                    .OrderBy(herb => herb.Position.DistanceToSquared(thing.Position)))
                {
                    if (herb.IsForbidden(pawn) || !vanillaJob.bill.ingredientFilter.Allows(herb) ||
                        herb.Position.DistanceTo(thing.Position) > vanillaJob.bill.ingredientSearchRadius ||
                        !pawn.CanReserveAndReach(herb, PathEndMode.ClosestTouch, Danger.Some))
                        continue;
                    int take = System.Math.Min(remaining, herb.stackCount);
                    targets.Add(herb);
                    counts.Add(take);
                    remaining -= take;
                    if (remaining == 0)
                        break;
                }
                if (remaining > 0)
                {
                    JobFailReason.Is("Not enough accessible herbal medicine for this corpse's material quantity.");
                    return null;
                }
                vanillaJob.targetQueueB = targets;
                vanillaJob.countQueue = counts;
            }
            vanillaJob.def = DefDatabase<JobDef>.GetNamed("GF_ExtractCorpseBiomass");
            vanillaJob.targetB = corpse;
            return vanillaJob;
        }
    }
}
