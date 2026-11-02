namespace GeneFoundry.Components
{
    public static class GeneVatAnimationTiming
    {
        public const int TicksPerFrame = 15;
        public const int CycleTicks = 210;
        public static int FrameAt(int elapsedTicks)
        {
            int step = System.Math.Max(0, elapsedTicks) / TicksPerFrame % 14;
            return step <= 7 ? step : 14 - step;
        }
    }
}
