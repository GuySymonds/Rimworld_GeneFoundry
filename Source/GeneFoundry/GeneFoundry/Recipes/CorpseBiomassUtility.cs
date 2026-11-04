using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GeneFoundry.Recipes
{
    public static class CorpseBiomassUtility
    {
        public static bool CanProcess(Corpse corpse)
        {
            if (corpse == null || corpse.InnerPawn == null)
                return false;
            return CanProcess(corpse.InnerPawn.RaceProps);
        }

        public static bool CanProcess(RaceProperties race)
        {
            return race != null && race.FleshType != null && BiomassCalculations.IsOrganicCorpse(race.FleshType.isOrganic,
                race.FleshType == FleshTypeDefOf.Mechanoid, race.FleshType == FleshTypeDefOf.Drone);
        }

        public static void ConfigureRecipeFilters(RecipeDef recipe, IEnumerable<ThingDef> races)
        {
            var corpses = races.Where(race => CanProcess(race.race) && race.race.corpseDef != null)
                .Select(race => race.race.corpseDef).Distinct().ToList();
            // Replace broad category membership with concrete eligible corpse definitions.
            // Keep special filters (fresh/rotten, implants, etc.) and catalyst allowances intact.
            var corpseFilter = recipe.ingredients[0].filter;
            foreach (var def in corpseFilter.AllowedThingDefs.ToList()) corpseFilter.SetAllow(def, false);
            foreach (var def in corpses) corpseFilter.SetAllow(def, true);
            foreach (var def in recipe.fixedIngredientFilter.AllowedThingDefs.ToList())
                recipe.fixedIngredientFilter.SetAllow(def, false);
            foreach (var ingredient in recipe.ingredients)
                foreach (var def in ingredient.filter.AllowedThingDefs)
                    recipe.fixedIngredientFilter.SetAllow(def, true);
            foreach (var def in recipe.defaultIngredientFilter.AllowedThingDefs.ToList())
                if (!recipe.fixedIngredientFilter.Allows(def)) recipe.defaultIngredientFilter.SetAllow(def, false);
        }

        public static float RecoverableMeat(Corpse corpse)
        {
            // Retain body size and missing-part adjustments, but omit vanilla's
            // post-processing curve that boosts meat recovery from small animals.
            return Math.Max(0f, corpse.InnerPawn.GetStatValue(StatDefOf.MeatAmount, false));
        }

        public static int Yield(float meatAmount, RotStage stage, bool enzymatic = false)
        {
            // Include remaining organs, bones and hide, even when limbs are missing.
            return BiomassCalculations.CorpseYield(meatAmount, DecayMultiplier(stage), enzymatic);
        }

        public static float DecayMultiplier(RotStage stage)
        {
            return stage == RotStage.Dessicated ? 0.10f : stage == RotStage.Rotting ? 0.50f : 1f;
        }

        public static int WorkTicks(float meatAmount, float workSpeed, RotStage stage)
        {
            GeneFoundrySettings settings = GeneFoundryMod.Settings;
            return (BiomassCalculations.CorpseWorkTicks(meatAmount, workSpeed, DecayMultiplier(stage),
                settings.corpseMinutesPer50Material, settings.corpseSetupMinutes, settings.corpseHandlingPercent) + 1) / 2;
        }
    }

    public class RecipeDef_BiomassCorpse : RecipeDef
    {
        public override void ResolveReferences()
        {
            base.ResolveReferences();
            CorpseBiomassUtility.ConfigureRecipeFilters(this, DefDatabase<ThingDef>.AllDefsListForReading);
        }
    }

    public class SpecialThingFilterWorker_UnsafeBiomassCorpse : SpecialThingFilterWorker
    {
        public override bool Matches(Thing thing) =>
            thing is Corpse corpse && !CorpseBiomassUtility.CanProcess(corpse);

        public override bool CanEverMatch(ThingDef def) => def.IsCorpse;
    }
}
