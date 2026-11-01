using System.Collections.Generic;
using System.Linq;
using GeneFoundry.Fabrication;
using RimWorld;
using Verse;
using Verse.AI;

namespace GeneFoundry.WorkGivers
{
    public class WorkGiver_FabricateGenePack : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("GF_GeneFabricator"));
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            var bench = thing as Building_GeneFabricator;
            if (bench == null || bench.Faction != pawn.Faction || !bench.Powered || thing.IsForbidden(pawn) || !FabricationSkills.Qualified(pawn) ||
                !pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Some)) return null;
            var current = bench.Unfinished;
            if (current != null && current.CanBeWorkedBy(pawn) && !bench.AtWorkSurface(current) &&
                bench.CanRetrieve(current, pawn) && (bench.ActiveBill == null || bench.ActiveBill.ShouldDoNow()))
                return HandlingJob("GF_ResumeFabrication", bench, current);
            if (!bench.CanWork || !bench.CanBeWorkedBy(pawn)) return NextJob(pawn, bench);
            if (bench.Unfinished == null)
            {
                foreach (ThingDef def in new[] { Building_GeneFabricator.BiomassDef, Building_GeneFabricator.CapsuleDef })
                {
                    int missing = bench.Missing(def);
                    if (missing <= 0) continue;
                    Thing ingredient = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def),
                        PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f,
                        t => !t.IsForbidden(pawn) && pawn.CanReserve(t));
                    if (ingredient == null) return NextJob(pawn, bench);
                    var delivery = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("GF_DeliverFabrication"), thing, ingredient);
                    delivery.countQueue = new List<int> { bench.Generation };
                    delivery.count = System.Math.Min(missing, ingredient.stackCount);
                    return delivery;
                }
            }
            else if (!bench.Unfinished.Valid) return null;
            var work = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("GF_FabricateGenePack"), thing);
            work.countQueue = new List<int> { bench.Generation };
            return work;
        }
        private Job NextJob(Pawn pawn, Building_GeneFabricator bench)
        {
            var manual = bench.ParkedManualFor(pawn);
            var bill = bench.NextBillFor(pawn);
            if (manual == null && bill == null) return null;
            if (bench.Pending)
            {
                var current = bench.Unfinished;
                if (current != null && (!current.Spawned || !pawn.CanReserveAndReach(current, PathEndMode.ClosestTouch, Danger.Some))) return null;
                var clear = HandlingJob("GF_SetAsideFabrication", bench, current);
                clear.bill = bill;
                return clear;
            }
            if (manual != null) return HandlingJob("GF_ResumeFabrication", bench, manual);
            if (bill.HasStarted) return HandlingJob("GF_ResumeFabrication", bench, bill.unfinished);
            var job = HandlingJob("GF_StartFabricationBill", bench, null);
            job.bill = bill;
            return job;
        }
        private static Job HandlingJob(string defName, Building_GeneFabricator bench, UnfinishedGenePack item)
        {
            var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(defName), bench);
            if (item != null) job.targetB = item;
            job.count = 1;
            job.countQueue = new List<int> { bench.Generation };
            return job;
        }
    }
}
