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
        private int recoveryTicks;
        private Hediff recoveryPart;
        private Corpse Corpse => job.GetTarget(TargetIndex.B).Thing as Corpse;

        public override string GetReport()
        {
            return recoveryPart == null ? base.GetReport() : $"recovering {recoveryPart.Label}.";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref processingTicks, "gfCorpseProcessingTicks", 0);
            Scribe_Values.Look(ref recoveryTicks, "gfRecoveryTicks", 0);
            Scribe_References.Look(ref recoveryPart, "gfRecoveryPart");
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
                !ArtificialPartRecovery.CanRecoverAll(pawn, Corpse.InnerPawn) ||
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
            });

            Toil beginRecovery = Toils_General.Do(() =>
            {
                if (recoveryPart == null || !Corpse.InnerPawn.health.hediffSet.hediffs.Contains(recoveryPart))
                {
                    recoveryPart = ArtificialPartRecovery.NextPart(Corpse.InnerPawn);
                    recoveryTicks = recoveryPart == null ? 0 : ArtificialPartRecovery.WorkTicks(pawn, recoveryPart);
                }
            });
            Toil beginExtraction = Toils_General.Do(() =>
            {
                if (processingTicks <= 0)
                    processingTicks = CorpseBiomassUtility.WorkTicks(
                        CorpseBiomassUtility.RecoverableMeat(Corpse),
                        pawn.GetStatValue(DefDatabase<StatDef>.GetNamed("GF_BiomassExtractionSpeed")), Corpse.GetRotStage());
            });

            // A fixed loop of toils keeps save/load indices stable as parts are removed.
            yield return beginRecovery;
            yield return Toils_Jump.JumpIf(beginExtraction, () => recoveryPart == null);
            Toil recovery = Toils_General.Wait(1);
            recovery.initAction = () => ticksLeftThisToil = recoveryTicks;
            recovery.WithProgressBar(TargetIndex.A,
                () => 1f - ticksLeftThisToil / (float)System.Math.Max(1, recoveryTicks));
            recovery.AddPreTickIntervalAction(delta => pawn.skills.Learn(SkillDefOf.Medicine, 0.1f * delta));
            yield return recovery;
            yield return Toils_General.Do(() =>
            {
                if (recoveryPart != null)
                    ArtificialPartRecovery.Complete(pawn, Corpse, job.targetA.Thing, recoveryPart);
                recoveryPart = null;
                recoveryTicks = 0;
            });
            yield return Toils_Jump.Jump(beginRecovery);
            yield return beginExtraction;

            Toil processing = Toils_General.Wait(1);
            processing.initAction = () => ticksLeftThisToil = processingTicks;
            processing.WithProgressBar(TargetIndex.A,
                () => 1f - ticksLeftThisToil / (float)System.Math.Max(1, processingTicks));
            processing.AddPreTickIntervalAction(delta => GeneVatSkills.Learn(pawn, delta));
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
