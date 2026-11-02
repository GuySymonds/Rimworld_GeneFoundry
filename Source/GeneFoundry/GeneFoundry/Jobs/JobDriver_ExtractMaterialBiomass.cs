using System.Collections.Generic;
using GeneFoundry.Recipes;
using GeneFoundry.Components;
using Verse;
using Verse.AI;

namespace GeneFoundry.Jobs
{
    public class JobDriver_ExtractMaterialBiomass : JobDriver_DoBill, IGeneVatProcessing
    {
        private int completedOutput = -1;
        public float CompletionEfficiency => completedOutput < 0 ? 1f :
            BiomassCalculations.MaterialYieldEfficiency(completedOutput / (float)job.RecipeDef.products[0].count);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref completedOutput, "gfCompletedOutput", -1);
        }

        public bool ProcessingBiomass => HaveCurToil && CurToil.debugName == nameof(Toils_Recipe.DoRecipeWork);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            foreach (Toil toil in base.MakeNewToils())
            {
                GeneVatIngredientPlacement.Configure(toil);
                // Verified against RimWorld 1.6's Toils_Recipe.DoRecipeWork.
                // Preserve vanilla hauling, interruptions, completion and output storage.
                if (toil.debugName == nameof(Toils_Recipe.DoRecipeWork))
                {
                    toil.AddPreTickIntervalAction(delta => GeneVatSkills.Learn(pawn, delta));
                    toil.AddPreTickIntervalAction(delta => job.targetA.Thing.TryGetComp<CompGeneVatRotStink>()?.NotifyExtraction(pawn, delta));
                    toil.AddPreTickIntervalAction(delta => job.targetA.Thing.TryGetComp<CompGeneVatAnimation>()?.NotifyProcessing(pawn));
                }
                if (toil.debugName == nameof(Toils_Recipe.FinishRecipeAndStartStoringProduct))
                {
                    var finish = toil.initAction;
                    toil.initAction = () =>
                    {
                        if (!GeneVatIngredientPlacement.HasFullRecipeCost(job))
                        {
                            EndJobWith(JobCondition.Incompletable);
                            return;
                        }
                        if (completedOutput < 0)
                            completedOutput = GeneVatSkills.Output(pawn, job.RecipeDef.products[0].count);
                        finish();
                    };
                }
                yield return toil;
            }
        }
    }
}
