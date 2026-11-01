using System;

namespace GeneFoundry.Recipes
{
    // Pure calculations: no game objects, randomness or mutable global settings.
    public static class BiomassCalculations
    {
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

        public static int CorpseYield(float material, float decayMultiplier, float expertMultiplier = 1f)
        {
            return Math.Max(1, (int)Math.Floor(Math.Max(0f, material) * 0.625d * decayMultiplier * expertMultiplier));
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
