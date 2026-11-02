namespace GeneFoundry.Components
{
    public static class GeneVatAnimationTiming
    {
        public const float TicksPerFrame = 6f;
        public const int CycleTicks = 84;
        public static int FrameAt(int elapsedTicks)
        {
            // One frame per six ticks: 25% faster than the previous 7.5-tick cadence.
            int step = System.Math.Max(0, elapsedTicks) / 6 % 14;
            return step <= 7 ? step : 14 - step;
        }
    }
}
