using System.Collections.Generic;
using GeneFoundry.Recipes;
using Verse.AI;

namespace GeneFoundry.Jobs
{
    public class JobDriver_ExtractMaterialBiomass : JobDriver_DoBill
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            foreach (Toil toil in base.MakeNewToils())
            {
                // Verified against RimWorld 1.6's Toils_Recipe.DoRecipeWork.
                // Preserve vanilla hauling, interruptions, completion and output storage.
                if (toil.debugName == nameof(Toils_Recipe.DoRecipeWork))
                    toil.AddPreTickIntervalAction(delta => GeneVatSkills.Learn(pawn, delta));
                yield return toil;
            }
        }
    }
}
