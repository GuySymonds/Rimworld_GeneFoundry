using System.Collections.Generic;
using GeneFoundry.Recipes;
using RimWorld;
using Verse;
using Verse.AI;

namespace GeneFoundry.Jobs
{
    public class JobDriver_ExtractCorpseBiomass : JobDriver
    {
        private int processingTicks;
        private Corpse Corpse => job.GetTarget(TargetIndex.B).Thing as Corpse;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref processingTicks, "gfCorpseProcessingTicks", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) &&
                pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOn(() => job.bill == null || job.bill.DeletedOrDereferenced ||
                !job.bill.ShouldDoNow() || !CorpseBiomassUtility.CanProcess(Corpse) ||
                !(job.targetA.Thing is Building_WorkTable table) || !table.UsableForBillsAfterFueling());

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            yield return Toils_General.Do(() =>
            {
                if (!pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing dropped) ||
                    dropped != Corpse)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                // Vanilla stripping preserves the actual items, their condition and taint.
                Corpse.Strip(false);
                if (processingTicks <= 0)
                    processingTicks = CorpseBiomassUtility.WorkTicks(
                        CorpseBiomassUtility.RecoverableMeat(Corpse),
                        pawn.GetStatValue(DefDatabase<StatDef>.GetNamed("CookSpeed")), Corpse.GetRotStage());
            });

            Toil processing = Toils_General.Wait(1);
            processing.initAction = () => ticksLeftThisToil = processingTicks;
            processing.WithProgressBar(TargetIndex.A,
                () => 1f - ticksLeftThisToil / (float)System.Math.Max(1, processingTicks));
            processing.AddPreTickIntervalAction(delta => pawn.skills.Learn(SkillDefOf.Cooking, 0.1f * delta));
            yield return processing;

            yield return Toils_General.Do(() =>
            {
                Corpse corpse = Corpse;
                Pawn source = corpse.InnerPawn;
                int amount = CorpseBiomassUtility.Yield(
                    CorpseBiomassUtility.RecoverableMeat(corpse), corpse.GetRotStage());
                ThingDef biomassDef = DefDatabase<ThingDef>.GetNamed("GF_RawBiomass");

                // Vanilla body-part definitions determine horns, tusks and modded products.
                // Flesh and leather are consumed by extraction instead of being output again.
                foreach (Thing product in source.ButcherProducts(pawn, 1f))
                {
                    if (product.def == source.RaceProps.meatDef || product.def == source.RaceProps.leatherDef)
                        product.Destroy(DestroyMode.Vanish);
                    else
                        GenPlace.TryPlaceThing(product, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                }

                while (amount > 0)
                {
                    Thing output = ThingMaker.MakeThing(biomassDef);
                    output.stackCount = System.Math.Min(amount, biomassDef.stackLimit);
                    amount -= output.stackCount;
                    GenPlace.TryPlaceThing(output, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                }

                job.bill.Notify_IterationCompleted(pawn, new List<Thing> { corpse });
                corpse.Destroy(DestroyMode.Vanish);
            });
        }
    }
}
