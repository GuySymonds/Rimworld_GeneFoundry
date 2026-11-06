using System;

namespace GeneFoundry.Recipes
{
    // Pure calculations: no game objects, randomness or mutable global settings.
    public static class BiomassCalculations
    {
        public static float CorpseRotStinkRisk(int cooking) =>
            (float)(0.15d * (1d - Progress(cooking, 0, 10)));

        public static float LiquidationVisualProgress(int total, int remaining) => total <= 0 ? 0f :
            Math.Max(0f, Math.Min(1f, 1f - remaining / (float)total));
        public static float LiquidationVisualScale(float width, float height, float availableWidth, float availableHeight)
        {
            if (width <= 0f || height <= 0f || availableWidth <= 0f || availableHeight <= 0f) return 1f;
            // Native mesh bounds include transparent sprite padding. Allow twice the
            // basin dimensions while retaining natural size as the upper limit.
            return Math.Min(1f, Math.Min(2f * availableWidth / width, 2f * availableHeight / height));
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

        public const float AdequateLight = .30f;
        public static float DimLightAdjustment(float light, int penaltyPoints = 10) =>
            Math.Max(0f, Math.Min(1f, 1f - light / AdequateLight)) *
                Math.Max(0, Math.Min(50, penaltyPoints)) / 100f;

        public static float ProcessingEnvironmentAdjustment(double cleanliness, bool exposed, float light,
            bool includeCleanliness, bool cleanlinessEnabled = true, int dirtyPenaltyPoints = 15,
            int exposurePenaltyPoints = 10, int dimLightPenaltyPoints = 10)
        {
            float room = exposed ? Math.Max(0, Math.Min(50, exposurePenaltyPoints)) / 100f :
                RefinementCleanlinessAdjustment(cleanliness, false, includeCleanliness && cleanlinessEnabled, dirtyPenaltyPoints);
            return Math.Min(1f, room + DimLightAdjustment(light, dimLightPenaltyPoints));
        }

        private static double Progress(int level, int minimum, int maximum)
        {
            return Math.Max(0d, Math.Min(1d, (level - minimum) / (double)(maximum - minimum)));
        }

        public static double RefinementSkill(int intellectual) =>
            Math.Max(5d, Math.Min(20d, intellectual));

        public static float RefinementFailureChance(int intellectual)
        {
            double skill = RefinementSkill(intellectual);
            return (float)(skill <= 10d
                ? 0.30d - (skill - 5d) * 0.05d
                : 0.05d * (20d - skill) / 10d);
        }

        public static int RefinementPreparationTicks(int intellectual) =>
            (int)Math.Round((90d - 4d * (RefinementSkill(intellectual) - 5d)) * 2500d / 60d);

        public static int RefinementUsefulProcessors(int durationMinutes, int minimumMinutes, float reductionMinutes)
        {
            int duration = Math.Max(1, durationMinutes);
            int minimum = Math.Max(1, Math.Min(duration, minimumMinutes));
            return reductionMinutes <= 0f ? 0 :
                (int)Math.Ceiling((duration - minimum) / (double)reductionMinutes - .00001d);
        }

        public const float ExtractionXpPerTick = 0.1f;

        public static float ExpertProgress(int cooking) => (float)Progress(cooking, 10, 20);

        public static float ExtractionSkillSpeed(int cooking,
            int expertSpeedPercent = 200) =>
            1f + ExpertProgress(cooking) *
                (Math.Max(150, Math.Min(300, expertSpeedPercent)) / 100f - 1f);

        public static double ExtractionShortfall(int cooking) => 1d - Progress(cooking, 0, 10);

        public static int ExtractionOutput(int baseOutput, int cooking, double roll,
            float environmentPenalty = 0f)
        {
            double loss = ExtractionShortfall(cooking) *
                (0.5d + 0.5d * Math.Max(0d, Math.Min(1d, roll)));
            // Remove float representation noise before flooring whole-item output.
            loss = Math.Min(1d, loss + Math.Round(Math.Max(0f, Math.Min(1f, environmentPenalty)), 6));
            return Math.Max(1, (int)Math.Floor(baseOutput * (1d - loss) + .000001d));
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
            // Decimal factors keep exact dry-mass boundaries (including 10% decay)
            // from losing a whole item to binary floating-point representation.
            decimal dryMass = (decimal)Math.Max(0f, material) * .25m * 2.2m;
            return Math.Max(1, (int)Math.Floor(dryMass * (enzymatic ? .75m : .60m) *
                (decimal)decayMultiplier));
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
