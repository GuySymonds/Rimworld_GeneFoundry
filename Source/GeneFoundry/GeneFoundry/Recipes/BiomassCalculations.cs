using System;

namespace GeneFoundry.Recipes
{
    // Pure calculations: no game objects, randomness or mutable global settings.
    public static class BiomassCalculations
    {
        public static int EnzymaticCatalystCount(float material)
        {
            return Math.Max(1, (int)Math.Ceiling(Math.Max(0f, material) / 50d));
        }

        public static bool IsOrganicCorpse(bool flesh, bool mechanoid, bool drone)
        {
            return flesh && !mechanoid && !drone;
        }
        public static float RefinementCleanlinessAdjustment(double cleanliness, bool exposed,
            bool enabled = true, int dirtyPenaltyPoints = 15, int sterileReductionPoints = 5,
            int exposurePenaltyPoints = 15)
        {
            if (!enabled)
                return 0f;
            if (exposed)
                return Math.Max(0, Math.Min(50, exposurePenaltyPoints)) / 100f;
            if (cleanliness < 0d)
                return (float)(Math.Min(1d, -cleanliness / 2d) *
                    Math.Max(0, Math.Min(50, dirtyPenaltyPoints)) / 100d);
            return (float)(-Math.Min(1d, cleanliness / 0.6d) *
                Math.Max(0, Math.Min(30, sterileReductionPoints)) / 100d);
        }

        public static float CombinedRefinementFailure(float operatorRisk, float environmentAdjustment)
        {
            return Math.Max(0f, Math.Min(1f, operatorRisk + environmentAdjustment));
        }

        private static double Progress(int level, int minimum, int maximum)
        {
            return Math.Max(0d, Math.Min(1d, (level - minimum) / (double)(maximum - minimum)));
        }

        public static double RefinementSkillProgress(int intellectual, int crafting, int medicine)
        {
            return 0.60d * Progress(intellectual, 5, 10) + 0.25d * Progress(crafting, 3, 8) +
                0.15d * Progress(medicine, 2, 6);
        }

        public static double RefinementExpertProgress(int intellectual, int crafting, int medicine)
        {
            return 0.60d * Progress(intellectual, 10, 20) + 0.25d * Progress(crafting, 8, 20) +
                0.15d * Progress(medicine, 6, 20);
        }

        public static float RefinementFailureChance(int intellectual, int crafting, int medicine)
        {
            return (float)(0.30d * (1d - RefinementSkillProgress(intellectual, crafting, medicine)));
        }

        public static int RefinementPreparationTicks(int intellectual, int crafting, int medicine,
            int maximumMinutes, int minimumMinutes, int expertSpeedPercent = 200)
        {
            maximumMinutes = Math.Max(1, maximumMinutes);
            minimumMinutes = Math.Max(1, Math.Min(maximumMinutes, minimumMinutes));
            double minutes = maximumMinutes - (maximumMinutes - minimumMinutes) *
                RefinementSkillProgress(intellectual, crafting, medicine);
            double expertSpeed = 1d + RefinementExpertProgress(intellectual, crafting, medicine) *
                (Math.Max(150, Math.Min(300, expertSpeedPercent)) / 150d - 1d);
            return Math.Max(1, (int)Math.Round(minutes * 2500d / 60d / expertSpeed));
        }

        public static double RefinementExpectedOutput(int baseCount, int intellectual, int crafting,
            int medicine, int expertYieldPercent = 20)
        {
            return Math.Max(1, baseCount) * (1d + RefinementExpertProgress(intellectual, crafting, medicine) *
                Math.Max(0, Math.Min(50, expertYieldPercent)) / 100d);
        }

        public static int RoundRefinementOutput(double expected, double roll)
        {
            int whole = Math.Max(1, (int)Math.Floor(expected));
            return whole + (roll < expected - whole ? 1 : 0);
        }

        public const float CookingWeight = 0.4f;
        public const float MedicineWeight = 0.4f;
        public const float IntellectualWeight = 0.2f;
        public const float ExtractionXpPerTick = 0.1f;

        public static float ExpertProgress(int cooking, int medicine, int intellectual)
        {
            return (CookingWeight * Math.Max(0, Math.Min(10, cooking - 10)) +
                MedicineWeight * Math.Max(0, Math.Min(10, medicine - 10)) +
                IntellectualWeight * Math.Max(0, Math.Min(10, intellectual - 10))) / 10f;
        }

        public static float ExtractionSkillSpeed(int cooking, int medicine, int intellectual,
            int expertSpeedPercent = 200)
        {
            float weightedLevel = CookingWeight * Math.Max(0, Math.Min(10, cooking)) +
                MedicineWeight * Math.Max(0, Math.Min(10, medicine)) +
                IntellectualWeight * Math.Max(0, Math.Min(10, intellectual));
            float expertMaximum = Math.Max(150, Math.Min(300, expertSpeedPercent)) / 100f;
            return 0.5f + weightedLevel / 10f +
                ExpertProgress(cooking, medicine, intellectual) * (expertMaximum - 1.5f);
        }

        public static float ExpertYieldMultiplier(int cooking, int medicine, int intellectual,
            int expertYieldPercent = 20)
        {
            return 1f + ExpertProgress(cooking, medicine, intellectual) *
                Math.Max(0, Math.Min(50, expertYieldPercent)) / 100f;
        }

        public static float MaterialYieldEfficiency(float multiplier)
        {
            if (multiplier <= 1f)
                return 1f;
            // Vanilla rounds products upwards. Use the preceding float to prevent
            // e.g. 25 * 1.2 becoming 30.000002 and incorrectly producing 31.
            int bits = BitConverter.ToInt32(BitConverter.GetBytes(multiplier), 0);
            return BitConverter.ToSingle(BitConverter.GetBytes(bits - 1), 0);
        }

        public static int MedicalRecoveryTicks(float workAmount, float workSpeed)
        {
            return Math.Max(1, (int)Math.Ceiling(Math.Max(0f, workAmount) / Math.Max(0.01f, workSpeed)));
        }

        public static float WithoutCleanliness(float environmentFactor, float cleanlinessFactor)
        {
            return environmentFactor / Math.Max(0.01f, cleanlinessFactor);
        }

        public static int CorpseYield(float material, float decayMultiplier, float expertMultiplier = 1f,
            bool enzymatic = false)
        {
            return Math.Max(1, (int)Math.Floor(Math.Max(0f, material) * 0.625d * decayMultiplier *
                (enzymatic ? 1.5d : 1d) * expertMultiplier));
        }

        public static int CorpseWorkTicks(float material, float workSpeed, float decayMultiplier,
            float minutesPer50Material, float setupMinutes, int handlingPercent)
        {
            material = Math.Max(0f, material);
            double batchFactor = material <= 50f ? 1d :
                Math.Max(0.75d, Math.Pow(material / 50d, -0.1d));
            double handling = Math.Max(0, Math.Min(100, handlingPercent)) / 100d;
            double decayTimeFactor = handling + (1d - handling) * decayMultiplier;
            double freshMinutes = Math.Max(0f, setupMinutes) +
                material / 50d * Math.Max(0f, minutesPer50Material);
            return Math.Max(1, (int)Math.Ceiling(freshMinutes * 2500d / 60d * batchFactor *
                decayTimeFactor / Math.Max(0.01f, workSpeed)));
        }
    }
}
