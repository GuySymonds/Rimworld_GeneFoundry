using GeneFoundry.Components;
using RimWorld;
using Verse;
using Verse.AI;

namespace GeneFoundry.WorkGivers
{
    public class WorkGiver_RefineBiomass : WorkGiver_DoBill
    {
        public override Job JobOnThing(
            Pawn pawn,
            Thing thing,
            bool forced = false)
        {
            var refinery = thing.TryGetComp<CompBiomassRefiner>();

            if (refinery == null || !refinery.CanStartBatch())
                return null;

            // Let vanilla select an eligible bill and locate Neutroamine.
            Job billJob = base.JobOnThing(pawn, thing, forced);

            if (billJob == null)
                return null;

            // Preserve vanilla housekeeping jobs.
            if (billJob.def != JobDefOf.DoBill)
                return billJob;

            if (billJob.bill?.recipe?.defName !=
                "GF_StartBiomassRefinement")
                return null;

            if (billJob.targetQueueB == null ||
                billJob.targetQueueB.Count != 1)
                return null;

            Thing neutroamine = billJob.targetQueueB[0].Thing;

            if (neutroamine == null)
                return null;

            JobDef jobDef = DefDatabase<JobDef>.GetNamed(
                "GF_PrepareBiomassRefinement");

            Job job = JobMaker.MakeJob(jobDef, thing, neutroamine);

            job.count = 1;
            job.bill = billJob.bill;

            return job;
        }
    }
}