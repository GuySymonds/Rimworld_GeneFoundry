using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneFoundry.Fabrication
{
    // Manufacturing balance defaults, independent of research costs. Negative modded stats
    // are clamped per gene by the caller before summing.
    public static class FabricationCalculations
    {
        public const int BiomassPerGene = 10, BiomassPerComplexity = 5;
        public const int WorkPerGene = 2500, WorkPerComplexity = 1000;
        public const float XpPerTick = 0.1f;
        public static int CancellationRecovery(int paid) => (int)(Math.Max(0L, paid) * 75 / 100);
        public static long GeneBiomass(int complexity) =>
            BiomassPerGene + Math.Max(0, complexity) * (long)BiomassPerComplexity;
        public static int Biomass(IEnumerable<int> complexities)
        {
            // Price expensive genes first, discount each rank, then round the combined cost once.
            decimal total = complexities.Select(GeneBiomass).OrderByDescending(cost => cost)
                .Select((cost, rank) => (decimal)cost * (100 - Math.Min(rank, 5) * 10) / 100m).Sum();
            return checked((int)Math.Ceiling(total));
        }
        public static float Work(int genes, int complexity) =>
            Math.Max(0, genes) * (float)WorkPerGene + Math.Max(0, complexity) * (float)WorkPerComplexity;
        public static bool Qualified(int crafting) => crafting >= 10;
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static float Speed(int crafting) => 0.5f + Clamp(crafting, 0, 20) / 10f;
        public static float Failure(int crafting) => 0.30f * Clamp((20 - crafting) / 10f, 0, 1);
        public static float WeightedFailure(float riskWork, float workDone) =>
            workDone > 0 ? Clamp(riskWork / workDone, 0, 1f) : 0.30f;
    }
}
