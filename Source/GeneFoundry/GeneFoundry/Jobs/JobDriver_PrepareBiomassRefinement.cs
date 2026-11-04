using System.Collections.Generic;
using GeneFoundry.Components;
using RimWorld;
using Verse;
using Verse.AI;

namespace GeneFoundry.Jobs
{
    public class JobDriver_PrepareBiomassRefinement : JobDriver
    {
        private const TargetIndex RefineryIndex = TargetIndex.A;
        private const TargetIndex NeutroamineIndex = TargetIndex.B;

        // Match vanilla research's total base learning rate, split across operator skills.
        private const float PreparationXpPerTick = 0.1f;

        private int preparationTicks;
        public bool Preparing { get; private set; }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref preparationTicks, "gfPreparationTicks", 0);
        }

        private CompBiomassRefiner Refinery =>
            job.GetTarget(RefineryIndex)
               .Thing
               ?.TryGetComp<CompBiomassRefiner>();

        public override bool TryMakePreToilReservations(
            bool errorOnFailed)
        {
            if (!pawn.Reserve(
                job.GetTarget(RefineryIndex),
                job,
                1,
                -1,
                null,
                errorOnFailed))
                return false;

            return pawn.Reserve(
                job.GetTarget(NeutroamineIndex),
                job,
                1,
                1,
                null,
                errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(RefineryIndex);
            this.FailOnBurningImmobile(RefineryIndex);

            // Collect Neutroamine.
            yield return Toils_Goto.GotoThing(
                NeutroamineIndex,
                PathEndMode.ClosestTouch);

            yield return Toils_Haul.StartCarryThing(
                NeutroamineIndex);

            // Travel to the refinery.
            yield return Toils_Goto.GotoThing(
                RefineryIndex,
                PathEndMode.InteractionCell);

            // Capture the duration once per job and retain it across save/load.
            if (preparationTicks <= 0)
                preparationTicks = CompBiomassRefiner.CalculatePreparationTicks(pawn);

            // Perform manual preparation.
            Toil preparation = Toils_General.Wait(preparationTicks);
            preparation.AddPreInitAction(() =>
            {
                Preparing = true;
                Refinery?.NotifyPreparing(pawn, this);
            });
            preparation.AddFinishAction(() => Preparing = false);

            preparation.AddPreTickIntervalAction(delta =>
            {
                float experience = PreparationXpPerTick * delta;
                pawn.skills.Learn(SkillDefOf.Intellectual, experience * 0.60f);
                pawn.skills.Learn(SkillDefOf.Crafting, experience * 0.25f);
                pawn.skills.Learn(SkillDefOf.Medicine, experience * 0.15f);
            });

            preparation.WithProgressBarToilDelay(RefineryIndex);

            yield return preparation;

            // Initiate the automated reaction.
            yield return Toils_General.Do(() =>
            {
                Thing catalyst = pawn.carryTracker.CarriedThing;

                if (catalyst == null ||
                    catalyst.def.defName != "Neutroamine" ||
                    catalyst.stackCount < 1 ||
                    Refinery == null ||
                    job.bill == null ||
                    job.bill.DeletedOrDereferenced ||
                    !job.bill.ShouldDoNow())
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Recheck readiness before consuming anything.
                if (!Refinery.TryStartBatch(pawn))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // The reaction has started successfully.
                // Consume one unit of Neutroamine.
                Thing consumed = catalyst.SplitOff(1);
                consumed.Destroy(DestroyMode.Vanish);

                // Count one initiated batch against the bill.
                job.bill.Notify_IterationCompleted(
                    pawn,
                    new List<Thing>());
            });
        }
    }
}
