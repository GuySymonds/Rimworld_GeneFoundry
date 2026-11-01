using System;

namespace GeneFoundry.Research
{
    public static class GeneResearchCalculations
    {
        public static int CategoryCost(int geneCount, long complexity, long archite,
            int baseCost = 500, int perGene = 100, int perComplexity = 200, int perArchite = 500,
            int costPercent = 25, int maximumCost = 4000, int minimumCost = 1000)
        {
            // Use wide arithmetic: large mod lists must not wrap into cheap/negative research.
            decimal cost = Math.Max(0, baseCost) + (decimal)Math.Max(0, geneCount) * Math.Max(0, perGene) +
                (decimal)Math.Min(int.MaxValue, Math.Max(0L, complexity)) * Math.Max(0, perComplexity) +
                (decimal)Math.Min(int.MaxValue, Math.Max(0L, archite)) * Math.Max(0, perArchite);
            decimal scaled = Math.Ceiling(cost * Math.Max(0, costPercent) / 100m);
            int minimum = Math.Max(1, minimumCost);
            int maximum = Math.Max(minimum, maximumCost);
            return (int)Math.Max(minimum, Math.Min(maximum, scaled));
        }

        public static bool IsUnlocked(bool fabricationComplete, bool categoryComplete,
            int architeCost, bool archogeneticsComplete)
        {
            return fabricationComplete && categoryComplete && (architeCost <= 0 || archogeneticsComplete);
        }
    }
}
