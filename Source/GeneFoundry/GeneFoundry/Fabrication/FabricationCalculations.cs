using System;

namespace GeneFoundry.Fabrication
{
    // Manufacturing balance defaults, independent of research costs. Negative modded stats
    // are clamped per gene by the caller before summing.
    public static class FabricationCalculations
    {
        public const int BiomassPerGene = 10, BiomassPerComplexity = 5;
        public const int WorkPerGene = 2500, WorkPerComplexity = 1000;
        public const float IntellectualWeight = 0.60f, MedicineWeight = 0.25f, CraftingWeight = 0.15f;
        public const float XpPerTick = 0.1f;
        public const int DiscountPerAdditionalGene = 10, MaximumDiscount = 50;
        public static int BiomassDiscountPercent(int genes) =>
            (int)Math.Min(MaximumDiscount, Math.Max(0L, (long)genes - 1) * DiscountPerAdditionalGene);
        public static int Biomass(int genes, int complexity)
        {
            long baseCost = Math.Max(0, genes) * (long)BiomassPerGene + Math.Max(0, complexity) * (long)BiomassPerComplexity;
            // Integer ceiling avoids floating point rounding granting or charging an extra unit.
            return checked((int)((baseCost * (100 - BiomassDiscountPercent(genes)) + 99) / 100));
        }
        public static float Work(int genes, int complexity) =>
            Math.Max(0, genes) * (float)WorkPerGene + Math.Max(0, complexity) * (float)WorkPerComplexity;
        public static bool Qualified(int intellectual, int medicine, int crafting) =>
            intellectual >= 10 && medicine >= 6 && crafting >= 4;
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static float Speed(int intellectual, int medicine, int crafting) =>
            0.5f + (Clamp(intellectual, 0, 20) * IntellectualWeight +
                Clamp(medicine, 0, 20) * MedicineWeight + Clamp(crafting, 0, 20) * CraftingWeight) / 10f;
        public static float Failure(int intellectual, int medicine, int crafting) =>
            0.30f * (Clamp((16 - intellectual) / 6f, 0, 1) * IntellectualWeight +
                Clamp((12 - medicine) / 6f, 0, 1) * MedicineWeight +
                Clamp((10 - crafting) / 6f, 0, 1) * CraftingWeight);
        public static float WeightedFailure(float riskWork, float workDone) =>
            workDone > 0 ? Clamp(riskWork / workDone, 0, 0.30f) : 0.30f;
    }
}
