using RimWorld;
using Verse;
using Verse.AI;
using GeneFoundry.Recipes;

namespace GeneFoundry.WorkGivers
{
    public class WorkGiver_GeneVat : WorkGiver_DoBill
    {
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            Job vanillaJob = base.JobOnThing(pawn, thing, forced);
            if (vanillaJob == null || vanillaJob.def != JobDefOf.DoBill ||
                vanillaJob.bill?.recipe?.defName != "GF_ExtractBiomassCorpse")
                return vanillaJob;

            if (vanillaJob.targetQueueB == null || vanillaJob.targetQueueB.Count != 1)
                return null;
            Corpse corpse = vanillaJob.targetQueueB[0].Thing as Corpse;
            if (!CorpseBiomassUtility.CanProcess(corpse))
                return null;

            Job result = JobMaker.MakeJob(
                DefDatabase<JobDef>.GetNamed("GF_ExtractCorpseBiomass"), thing, corpse);
            result.count = 1;
            result.bill = vanillaJob.bill;
            return result;
        }
    }
}
