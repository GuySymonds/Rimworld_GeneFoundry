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

        // Identify vanilla's visual-only WithEffect callback through its public
        // builder, rather than relying on compiler-generated names or list indices
        // on the actual recipe toil. Keep sounds, progress, work and cleanup intact.
        private static readonly System.Reflection.MethodInfo WorkEffectTick = FindWorkEffectTick();
        private static System.Reflection.MethodInfo FindWorkEffectTick()
        {
            Toil probe = ToilMaker.MakeToil("GeneFoundryWorkEffectSignature");
            probe.WithEffect(() => null, TargetIndex.A);
            var method = probe.preTickActions[0].Method;
            probe.Clear();
            return method;
        }

        internal static void ConfigureWorkEffects(Toil toil)
        {
            if (toil.preTickActions == null) return;
            for (int index = 0; index < toil.preTickActions.Count; index++)
            {
                var effect = toil.preTickActions[index];
                if (effect.Method != WorkEffectTick) continue;
                toil.preTickActions[index] = () =>
                {
                    if (!Graphic_StyledMachine.UsesSimple(toil.actor.CurJob.targetA.Thing)) effect();
                };
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !GeneVatSkills.Qualified(pawn));
            foreach (Toil toil in base.MakeNewToils())
            {
                GeneVatIngredientPlacement.Configure(toil);
                // Verified against RimWorld 1.6's Toils_Recipe.DoRecipeWork.
                // Preserve vanilla hauling, interruptions, completion and output storage.
                if (toil.debugName == nameof(Toils_Recipe.DoRecipeWork))
                {
                    ConfigureWorkEffects(toil);
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
