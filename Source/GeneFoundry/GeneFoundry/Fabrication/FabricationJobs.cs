using System.Collections.Generic;
using System.Linq;
using GeneFoundry.Fabrication;
using RimWorld;
using Verse;
using Verse.AI;

namespace GeneFoundry.Jobs
{
    public class JobDriver_DeliverFabrication : JobDriver
    {
        private Building_GeneFabricator Bench => job.targetA.Thing as Building_GeneFabricator;
        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) &&
            pawn.Reserve(job.targetB, job, 1, job.count, null, errorOnFailed);
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => Bench == null || !Bench.CanWork || job.countQueue == null || job.countQueue.Count != 1 || job.countQueue[0] != Bench.Generation || Bench.Unfinished != null);
            this.FailOnBurningImmobile(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Do(() => { if (!Bench.AcceptDelivery(pawn)) EndJobWith(JobCondition.Incompletable); });
        }
    }
    public class JobDriver_FabricateGenePack : JobDriver
    {
        private Building_GeneFabricator Bench => job.targetA.Thing as Building_GeneFabricator;
        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) &&
            (Bench?.Unfinished == null || !Bench.Unfinished.Spawned || pawn.Reserve(Bench.Unfinished, job, 1, 1, null, errorOnFailed));
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOn(() => Bench == null || !Bench.CanWork || job.countQueue == null || job.countQueue.Count != 1 || job.countQueue[0] != Bench.Generation || !FabricationSkills.Qualified(pawn) || !Bench.CanBeWorkedBy(pawn));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            var work = ToilMaker.MakeToil("FabricateGenePack");
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.initAction = () =>
            {
                if (!Bench.TryCommit(pawn) || !pawn.Reserve(Bench.Unfinished, job, 1, 1)) EndJobWith(JobCondition.Incompletable);
            };
            work.tickIntervalAction = delta =>
            {
                pawn.rotationTracker.FaceTarget(Bench);
                Bench.Work(pawn, delta);
                if (!Bench.Pending) ReadyForNextToil();
            };
            work.WithProgressBar(TargetIndex.A, () => Bench?.Unfinished == null ? 0 : Bench.Unfinished.workDone / Bench.Unfinished.totalWork);
            yield return work;
        }
    }
}
