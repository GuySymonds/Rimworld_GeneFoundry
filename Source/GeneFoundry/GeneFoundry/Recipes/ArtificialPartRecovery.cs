using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GeneFoundry.Recipes
{
    public static class ArtificialPartRecovery
    {
        private static readonly List<Thing> NoMedicine = new List<Thing>();

        public static Hediff NextPart(Pawn source)
        {
            // Recover nested implants before removing an enclosing artificial limb.
            return source.health.hediffSet.hediffs
                .Where(hediff => hediff.def.countsAsAddedPartOrImplant &&
                    !hediff.def.organicAddedBodypart &&
                    (hediff.Part == null || !source.health.hediffSet.PartIsMissing(hediff.Part)))
                .OrderByDescending(hediff => Depth(hediff.Part)).FirstOrDefault();
        }

        private static int Depth(BodyPartRecord part)
        {
            int depth = 0;
            while (part != null) { depth++; part = part.parent; }
            return depth;
        }

        private static RecipeDef RemovalRecipe(Hediff implant)
        {
            return DefDatabase<RecipeDef>.AllDefsListForReading
                .Where(recipe => recipe.removesHediff == implant.def && recipe.IsSurgery)
                .OrderBy(recipe => recipe.defName).FirstOrDefault() ??
                DefDatabase<RecipeDef>.GetNamed("RemoveBodyPart");
        }

        private static RecipeDef SurgeryRecipe(Hediff implant)
        {
            // Match installation difficulty when defined; otherwise use removal surgery.
            return DefDatabase<RecipeDef>.AllDefsListForReading
                .Where(recipe => recipe.addsHediff == implant.def && recipe.IsSurgery &&
                    recipe.surgeryOutcomeEffect != null)
                .OrderBy(recipe => recipe.defName).FirstOrDefault() ?? RemovalRecipe(implant);
        }

        public static bool CanRecoverAll(Pawn surgeon, Pawn source)
        {
            foreach (Hediff implant in source.health.hediffSet.hediffs)
            {
                if (!implant.def.countsAsAddedPartOrImplant || implant.def.organicAddedBodypart ||
                    (implant.Part != null && source.health.hediffSet.PartIsMissing(implant.Part)))
                    continue;
                if (surgeon.skills.GetSkill(SkillDefOf.Medicine).TotallyDisabled ||
                    !SurgeryRecipe(implant).PawnSatisfiesSkillRequirements(surgeon))
                    return false;
            }
            return true;
        }

        public static int WorkTicks(Pawn surgeon, Hediff implant)
        {
            RecipeDef removal = RemovalRecipe(implant);
            float speed = removal.workSpeedStat == null ? 1f : surgeon.GetStatValue(removal.workSpeedStat);
            return BiomassCalculations.MedicalRecoveryTicks(removal.workAmount, speed);
        }

        public static float SuccessChance(Pawn surgeon, Pawn source, Thing workstation, Hediff implant)
        {
            RecipeDef recipe = SurgeryRecipe(implant);
            StatDef environmentStat = StatDefOf.SurgerySuccessChanceFactor;
            float cleanliness = workstation.GetRoom()?.GetStat(
                DefDatabase<RoomStatDef>.GetNamed("SurgerySuccessChanceCleanlinessFactor")) ?? 1f;
            float quality = BiomassCalculations.WithoutCleanliness(
                workstation.GetStatValue(environmentStat), cleanliness);

            // Reuse vanilla factors and their ordering, but never invoke patient injury outcomes.
            if (recipe.surgeryOutcomeEffect != null)
            {
                foreach (SurgeryOutcomeComp component in recipe.surgeryOutcomeEffect.comps)
                {
                    if (component is SurgeryOutcomeComp_MedicineQuality ||
                        component is SurgeryOutcomeComp_BedAndRoomQuality)
                        continue;
                    if (component.Affects(recipe, surgeon, source, implant.Part))
                        component.AffectQuality(recipe, surgeon, source, NoMedicine,
                            implant.Part, null, ref quality);
                }
            }
            return Math.Max(0f, Math.Min(1f, quality));
        }

        public static void Complete(Pawn surgeon, Corpse corpse, Thing workstation, Hediff implant)
        {
            Pawn source = corpse.InnerPawn;
            if (!source.health.hediffSet.hediffs.Contains(implant))
                return;

            bool success = Rand.Chance(SuccessChance(surgeon, source, workstation, implant));
            RecipeDef recipe = SurgeryRecipe(implant);
            // Consume an applicable vanilla surgery inspiration once, as surgery would.
            if (recipe.surgeryOutcomeEffect != null)
                foreach (SurgeryOutcomeComp component in recipe.surgeryOutcomeEffect.comps)
                    if (component is SurgeryOutcomeComp_Inspired &&
                        component.Affects(recipe, surgeon, source, implant.Part))
                        component.PreApply(0f, recipe, surgeon, source, NoMedicine, implant.Part, null);

            string label = implant.Label;
            ThingDef recoveredItem = implant.def.spawnThingOnRemoved;
            BodyPartRecord bodyPart = implant.Part;
            bool replacesNaturalPart = implant.def.addedPartProps != null;

            // Alter the corpse before output: a completed attempt can never be repeated.
            source.health.RemoveHediff(implant);
            if (replacesNaturalPart && bodyPart != null && !source.health.hediffSet.PartIsMissing(bodyPart))
                source.health.AddHediff(HediffDefOf.MissingBodyPart, bodyPart);

            if (success && recoveredItem != null)
            {
                Thing item = ThingMaker.MakeThing(recoveredItem);
                if (!GenPlace.TryPlaceThing(item, surgeon.Position, surgeon.Map, ThingPlaceMode.Near))
                    surgeon.inventory.innerContainer.TryAdd(item);
                Messages.Message($"GeneFoundry: Recovered {recoveredItem.label}.", corpse,
                    MessageTypeDefOf.PositiveEvent, false);
            }
            else if (!success && recoveredItem != null)
            {
                Messages.Message($"GeneFoundry: {label} was destroyed during recovery.", corpse,
                    MessageTypeDefOf.NegativeEvent, false);
            }
        }
    }
}
