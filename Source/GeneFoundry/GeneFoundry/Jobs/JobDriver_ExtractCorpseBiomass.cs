using System.Collections.Generic;
using GeneFoundry.Recipes;
using RimWorld;
using Verse;
using Verse.AI;

namespace GeneFoundry.Jobs
{
    public class JobDriver_ExtractCorpseBiomass : JobDriver_DoBill
    {
        private int processingTicks;
        private int recoveryTicks;
        private Hediff recoveryPart;
        private Corpse sourceCorpse;
        private int requiredCatalyst;
        private bool corpsePositioned;
        private bool Enzymatic => job.bill?.recipe?.defName == "GF_ExtractBiomassCorpseEnzymatic";
        private Corpse Corpse => sourceCorpse ?? job.GetTarget(TargetIndex.B).Thing as Corpse;

        public override string GetReport()
        {
            if (recoveryPart != null)
                return $"recovering {recoveryPart.Label}.";
            string report = base.GetReport();
            return Enzymatic && requiredCatalyst > 0 ? $"{report} Catalyst: {requiredCatalyst} herbal medicine." : report;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref processingTicks, "gfCorpseProcessingTicks", 0);
            Scribe_Values.Look(ref recoveryTicks, "gfRecoveryTicks", 0);
            Scribe_References.Look(ref recoveryPart, "gfRecoveryPart");
            Scribe_References.Look(ref sourceCorpse, "gfSourceCorpse");
            Scribe_Values.Look(ref requiredCatalyst, "gfRequiredCatalyst", 0);
            Scribe_Values.Look(ref corpsePositioned, "gfCorpsePositioned", false);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return base.TryMakePreToilReservations(errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOn(() => job.bill == null || job.bill.DeletedOrDereferenced ||
                !job.bill.ShouldDoNow() || Corpse == null || Corpse.Destroyed || !CorpseBiomassUtility.CanProcess(Corpse) ||
                (corpsePositioned && (!Corpse.Spawned || Corpse.Position != GeneVatIngredientPlacement.CorpseCell(job.targetA.Thing))) ||
                !job.bill.recipe.PawnSatisfiesSkillRequirements(pawn) ||
                !ArtificialPartRecovery.CanRecoverAll(pawn, Corpse.InnerPawn) ||
                !(job.targetA.Thing is Building_WorkTable table) || !table.UsableForBillsAfterFueling());

            yield return Toils_General.Do(() =>
            {
                if (sourceCorpse == null)
                    sourceCorpse = Corpse;
                if (Enzymatic && requiredCatalyst == 0)
                    requiredCatalyst = BiomassCalculations.EnzymaticCatalystCount(
                        CorpseBiomassUtility.RecoverableMeat(Corpse));
            });
            foreach (Toil collect in CollectIngredientsToils(TargetIndex.B, TargetIndex.A, TargetIndex.C))
            {
                GeneVatIngredientPlacement.Configure(collect, positionCorpse: true);
                yield return collect;
            }
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            yield return Toils_General.Do(() =>
            {
                IntVec3 cell = GeneVatIngredientPlacement.CorpseCell(job.targetA.Thing);
                if (!Corpse.Spawned || Corpse.Map != pawn.Map || !job.targetA.Thing.OccupiedRect().Contains(cell))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                foreach (Thing other in cell.GetThingList(pawn.Map))
                {
                    if (other is Corpse && other != Corpse)
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                }
                // Also handles a corpse already lying among the vat's ingredient cells.
                // Thing.Position updates the map's spatial registries for a spawned item.
                Corpse.Position = cell;
                corpsePositioned = true;
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
                if (Enzymatic && !TryConsumeCatalyst())
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                Corpse corpse = Corpse;
                Pawn source = corpse.InnerPawn;
                int amount = CorpseBiomassUtility.Yield(
                    CorpseBiomassUtility.RecoverableMeat(corpse), corpse.GetRotStage(),
                    GeneVatSkills.YieldMultiplier(pawn), Enzymatic);
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

        private bool TryConsumeCatalyst()
        {
            // Use only this job's delivered quantities, even if they merged into
            // existing stacks. Validate the full cost before consuming anything.
            var delivered = new Dictionary<Thing, int>();
            foreach (ThingCountClass entry in job.placedThings ?? new List<ThingCountClass>())
            {
                Thing herb = entry.thing;
                if (herb == null || herb.Destroyed || !herb.Spawned || herb.Map != pawn.Map ||
                    herb.def != ThingDefOf.MedicineHerbal)
                    continue;
                delivered.TryGetValue(herb, out int count);
                delivered[herb] = count + entry.Count;
            }
            int available = 0;
            foreach (var entry in delivered)
                available += System.Math.Min(entry.Value, entry.Key.stackCount);
            if (available < requiredCatalyst)
                return false;
            int remaining = requiredCatalyst;
            foreach (var entry in delivered)
            {
                int take = System.Math.Min(remaining, System.Math.Min(entry.Value, entry.Key.stackCount));
                if (take > 0)
                    entry.Key.SplitOff(take).Destroy(DestroyMode.Vanish);
                remaining -= take;
                if (remaining == 0)
                    break;
            }
            return true;
        }
    }
}
