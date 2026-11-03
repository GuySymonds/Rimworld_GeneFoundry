using System;

namespace GeneFoundry.Components
{
    public static class BiomassRefineryAnimation
    {
        public const int FillingTicksPerFrame = 10;
        public const int ProcessingTicksPerFrame = 5;
        public const int FillingDuration = 8 * FillingTicksPerFrame;

        // Material indices: clean=0, dirty=1, filling=2..9, processing=10..25.
        public static int FrameAt(bool processing, bool used, int elapsedTicks)
        {
            if (!processing) return used ? 1 : 0;
            int elapsed = Math.Max(0, elapsedTicks);
            if (elapsed < FillingDuration) return 2 + elapsed / FillingTicksPerFrame;
            return 10 + (elapsed - FillingDuration) / ProcessingTicksPerFrame % 16;
        }
    }
}
