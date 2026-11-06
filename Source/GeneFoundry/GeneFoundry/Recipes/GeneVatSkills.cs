using RimWorld;
using Verse;

namespace GeneFoundry.Recipes
{
    public static class GeneVatSkills
    {
        public static float SkillSpeed(Pawn pawn)
        {
            return BiomassCalculations.ExtractionSkillSpeed(
                pawn.skills.GetSkill(SkillDefOf.Cooking).Level,
                GeneFoundryMod.Settings.expertSpeedPercent);
        }

        public static int Output(Pawn pawn, int baseOutput) => BiomassCalculations.ExtractionOutput(
            baseOutput, pawn.skills.GetSkill(SkillDefOf.Cooking).Level, Rand.Value,
            ProcessingEnvironment.Adjustment(pawn.CurJob?.targetA.Thing, false));

        public static void Learn(Pawn pawn, int ticks)
        {
            float xp = BiomassCalculations.ExtractionXpPerTick * ticks;
            pawn.skills.Learn(SkillDefOf.Cooking, xp);
        }
    }

    public class StatPart_GeneVatSkills : StatPart
    {
        public override void TransformValue(StatRequest req, ref float value)
        {
            if (req.Thing is Pawn pawn && pawn.skills != null)
                value *= GeneVatSkills.SkillSpeed(pawn);
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!(req.Thing is Pawn pawn) || pawn.skills == null)
                return null;
            return $"Extraction skill (Cooking): x{GeneVatSkills.SkillSpeed(pawn):0.##}";
        }
    }

    public class StatPart_GeneVatYield : StatPart
    {
        public override void TransformValue(StatRequest req, ref float value)
        {
            if (req.Thing is Pawn pawn && pawn.skills != null)
                value *= (pawn.jobs?.curDriver as GeneFoundry.Jobs.JobDriver_ExtractMaterialBiomass)?.CompletionEfficiency ?? 1f;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!(req.Thing is Pawn pawn) || pawn.skills == null)
                return null;
            return "Operator wastage is rolled once at extraction completion.";
        }
    }
}
