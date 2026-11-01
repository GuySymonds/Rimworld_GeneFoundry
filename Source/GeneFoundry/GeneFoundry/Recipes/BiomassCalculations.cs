using System;

namespace GeneFoundry.Recipes
{
    // Pure calculations: no game objects, randomness or mutable global settings.
    public static class BiomassCalculations
    {
        public static int CorpseYield(float material, float decayMultiplier)
        {
            return Math.Max(1, (int)Math.Floor(Math.Max(0f, material) * 0.625d * decayMultiplier));
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
