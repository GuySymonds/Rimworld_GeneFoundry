using System.Collections.Generic;
using GeneFoundry.Fabrication;
using RimWorld;
using Verse;
using Verse.AI;

namespace GeneFoundry.Jobs
{
    public class JobDriver_HandleFabrication : JobDriver
    {
        private Building_GeneFabricator Bench => job.targetA.Thing as Building_GeneFabricator;
        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) &&
            (!job.targetB.HasThing || pawn.Reserve(job.targetB, job, 1, 1, null, errorOnFailed));
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOn(() => Bench == null || !Bench.Powered || job.countQueue == null ||
                job.countQueue.Count != 1 || job.countQueue[0] != Bench.Generation || !FabricationSkills.Qualified(pawn));
            if (job.def.defName == "GF_StartFabricationBill")
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
                yield return Toils_General.Do(() =>
                {
                    if (!Bench.TryStartBill(job.bill as Bill_GeneFabrication)) EndJobWith(JobCondition.Incompletable);
                });
                yield break;
            }
            if (!job.targetB.HasThing)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
                yield return Toils_General.Do(() =>
                {
                    if (!Bench.ReleaseUncommitted()) { EndJobWith(JobCondition.Incompletable); return; }
                    if (job.bill is Bill_GeneFabrication next && !next.HasStarted) Bench.TryStartBill(next);
                });
                yield break;
            }
            this.FailOnDestroyedOrNull(TargetIndex.B);
            bool settingAside = job.def.defName == "GF_SetAsideFabrication";
            this.FailOn(() => settingAside ? Bench.Unfinished != job.targetB.Thing :
                !((UnfinishedGenePack)job.targetB.Thing).CanBeWorkedBy(pawn));
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Do(() =>
            {
                var item = pawn.carryTracker.CarriedThing as UnfinishedGenePack;
                if (settingAside)
                {
                    if (item == null || !pawn.carryTracker.innerContainer.TryDrop(item, Bench.InteractionCell, pawn.Map, ThingPlaceMode.Near, out Thing dropped,
                            nearPlaceValidator: cell => !Bench.OccupiedRect().Contains(cell)) ||
                        !Bench.DetachUnfinished(item)) { EndJobWith(JobCondition.Incompletable); return; }
                    var next = job.bill as Bill_GeneFabrication;
                    if (next != null && !next.HasStarted) Bench.TryStartBill(next);
                }
                else if (!Bench.RestoreUnfinished(item, pawn)) EndJobWith(JobCondition.Incompletable);
            });
        }
    }
}
