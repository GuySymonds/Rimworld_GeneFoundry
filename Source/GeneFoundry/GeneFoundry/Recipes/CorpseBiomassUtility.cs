using System;
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
            RaceProperties race = corpse.InnerPawn.RaceProps;
            return BiomassCalculations.IsOrganicCorpse(race.IsFlesh && race.FleshType.isOrganic,
                race.IsMechanoid, race.IsDrone);
        }

        public static float RecoverableMeat(Corpse corpse)
        {
            // Retain body size and missing-part adjustments, but omit vanilla's
            // post-processing curve that boosts meat recovery from small animals.
            return Math.Max(0f, corpse.InnerPawn.GetStatValue(StatDefOf.MeatAmount, false));
        }

        public static int Yield(float meatAmount, RotStage stage, float expertMultiplier = 1f, bool enzymatic = false)
        {
            // Include remaining organs, bones and hide, even when limbs are missing.
            return BiomassCalculations.CorpseYield(meatAmount, DecayMultiplier(stage), expertMultiplier, enzymatic);
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

    public class SpecialThingFilterWorker_UnsafeBiomassCorpse : SpecialThingFilterWorker
    {
        public override bool Matches(Thing thing) =>
            thing is Corpse corpse && !CorpseBiomassUtility.CanProcess(corpse);

        public override bool CanEverMatch(ThingDef def) => def.IsCorpse;
    }
}
