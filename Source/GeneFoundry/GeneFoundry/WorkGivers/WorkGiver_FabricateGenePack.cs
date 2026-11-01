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
            if (bench == null || bench.Faction != pawn.Faction || !bench.CanWork || thing.IsForbidden(pawn) || !FabricationSkills.Qualified(pawn) ||
                !pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Some)) return null;
            if (bench.Unfinished == null)
            {
                foreach (ThingDef def in new[] { Building_GeneFabricator.BiomassDef, Building_GeneFabricator.CapsuleDef })
                {
                    int missing = bench.Missing(def);
                    if (missing <= 0) continue;
                    Thing ingredient = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def),
                        PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f,
                        t => !t.IsForbidden(pawn) && pawn.CanReserve(t));
                    if (ingredient == null) return null;
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
    }
}
