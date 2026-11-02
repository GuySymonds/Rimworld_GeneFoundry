using System;

namespace GeneFoundry.Recipes
{
    // Pure calculations: no game objects, randomness or mutable global settings.
    public static class BiomassCalculations
    {
        public static float CorpseRotStinkRisk(int cooking, int medicine, int intellectual) =>
            ((cooking < 10 ? 1 : 0) + (medicine < 10 ? 1 : 0) + (intellectual < 10 ? 1 : 0)) * 0.05f;

        public static float LiquidationVisualProgress(int total, int remaining) => total <= 0 ? 0f :
            Math.Max(0f, Math.Min(1f, 1f - remaining / (float)total));
        public static float LiquidationVisualScale(float width, float height, float availableWidth, float availableHeight)
        {
            if (width <= 0f || height <= 0f || availableWidth <= 0f || availableHeight <= 0f) return 1f;
            return Math.Min(1f, Math.Min(availableWidth / width, availableHeight / height));
        }
        public static int BubbleMassFrame(int elapsedTicks) => Math.Max(0, elapsedTicks) / 6 % 8;
        public static int LiquidationVisualStage(int actualStage, float progress) =>
            Math.Max(actualStage, progress >= 2f / 3f ? 2 : progress >= 1f / 3f ? 1 : 0);
        public static int EnzymaticCatalystCount(float material)
        {
            return Math.Max(1, (int)Math.Ceiling(Math.Max(0f, material) / 50d));
        }

        public static bool IsOrganicCorpse(bool flesh, bool mechanoid, bool drone)
        {
            return flesh && !mechanoid && !drone;
        }
        public static float RefinementCleanlinessAdjustment(double cleanliness, bool exposed,
            bool enabled = true, int dirtyPenaltyPoints = 15, int exposurePenaltyPoints = 10)
        {
            if (!enabled)
                return 0f;
            if (exposed)
                return Math.Max(0, Math.Min(50, exposurePenaltyPoints)) / 100f;
            if (cleanliness < 0d)
                return (float)(Math.Min(1d, -cleanliness / 2d) *
                    Math.Max(0, Math.Min(50, dirtyPenaltyPoints)) / 100d);
            return 0f;
        }

        public static float CombinedRefinementFailure(float operatorRisk, float environmentAdjustment)
        {
            return Math.Max(0f, Math.Min(1f, operatorRisk + environmentAdjustment));
        }

        private static double Progress(int level, int minimum, int maximum)
        {
            return Math.Max(0d, Math.Min(1d, (level - minimum) / (double)(maximum - minimum)));
        }

        public static double RefinementSkill(int intellectual, int crafting, int medicine) =>
            Math.Max(5d, Math.Min(20d, 0.60d * intellectual + 0.25d * crafting + 0.15d * medicine));

        public static float RefinementFailureChance(int intellectual, int crafting, int medicine) =>
            (float)(0.30d * (20d - RefinementSkill(intellectual, crafting, medicine)) / 15d);

        public static int RefinementPreparationTicks(int intellectual, int crafting, int medicine) =>
            (int)Math.Round((90d - 4d * (RefinementSkill(intellectual, crafting, medicine) - 5d)) * 2500d / 60d);

        public const float CookingWeight = 0.4f;
        public const float MedicineWeight = 0.4f;
        public const float IntellectualWeight = 0.2f;
        public const float ExtractionXpPerTick = 0.1f;

        public static float ExpertProgress(int cooking, int medicine, int intellectual) =>
            (float)(0.4d * Progress(cooking, 10, 20) + 0.4d * Progress(medicine, 6, 20) +
                0.2d * Progress(intellectual, 4, 20));

        public static float ExtractionSkillSpeed(int cooking, int medicine, int intellectual,
            int expertSpeedPercent = 200) =>
            1f + ExpertProgress(cooking, medicine, intellectual) *
                (Math.Max(150, Math.Min(300, expertSpeedPercent)) / 100f - 1f);

        public static double ExtractionShortfall(int cooking, int medicine, int intellectual) =>
            0.4d * (1d - Progress(cooking, 0, 10)) +
            0.4d * (1d - Progress(medicine, 0, 6)) +
            0.2d * (1d - Progress(intellectual, 0, 4));

        public static int ExtractionOutput(int baseOutput, int cooking, int medicine, int intellectual, double roll)
        {
            double loss = ExtractionShortfall(cooking, medicine, intellectual) *
                (0.5d + 0.5d * Math.Max(0d, Math.Min(1d, roll)));
            return Math.Max(1, (int)Math.Floor(baseOutput * (1d - loss)));
        }

        // Vanilla rounds products up: choose the preceding float to keep exact integer recovery.
        public static float MaterialYieldEfficiency(float multiplier)
        {
            if (multiplier >= 1f) return 1f;
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

        public static int CorpseYield(float material, float decayMultiplier, bool enzymatic = false)
        {
            return Math.Max(1, (int)Math.Floor(Math.Max(0f, material) * 1.10d * (enzymatic ? 0.50d : 0.40d) * decayMultiplier));
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
