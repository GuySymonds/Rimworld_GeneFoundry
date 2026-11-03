using System;
using System.IO;
using System.Buffers.Binary;
using GeneFoundry.Components;
using Xunit;

namespace GeneFoundry.Tests
{
    public class BiomassRefineryAnimationTests
    {
        [Fact]
        public void FillingRunsOnceThenAllSixteenFramesLoop()
        {
            for (int tick = 0; tick < 560; tick++)
            {
                int expected = tick < 80 ? 2 + tick / 10 : 10 + (tick - 80) / 5 % 16;
                Assert.Equal(expected, BiomassRefineryAnimation.FrameAt(true, true, tick));
            }
            Assert.Equal(2, BiomassRefineryAnimation.FrameAt(true, true, -1));
        }

        [Fact]
        public void InactiveStateDependsOnlyOnUsageAndDrawsDoNotAdvanceTime()
        {
            Assert.Equal(0, BiomassRefineryAnimation.FrameAt(false, false, 5000));
            Assert.Equal(1, BiomassRefineryAnimation.FrameAt(false, true, 5000));
            Assert.Equal(14, BiomassRefineryAnimation.FrameAt(true, true, 100));
            Assert.Equal(2, BiomassRefineryAnimation.FrameAt(true, true, 0));
            Assert.Equal(14, BiomassRefineryAnimation.FrameAt(true, true, 100));
        }

        [Fact]
        public void AllTwentySixAssetsShareAnRgbaCanvas()
        {
            string root = Path.Combine(AppContext.BaseDirectory, "Fixtures/Refinery");
            Assert.Equal(26, Directory.GetFiles(root, "*.png").Length);
            byte[] first = File.ReadAllBytes(Path.Combine(root, "CleanVat.png"));
            for (int frame = 0; frame < 26; frame++)
            {
                string name = frame == 0 ? "CleanVat" : frame == 1 ? "DirtyVat" :
                    frame < 10 ? $"VatFilling{frame - 2:00}" : $"VatProcess{frame - 10:00}";
                byte[] png = File.ReadAllBytes(Path.Combine(root, name + ".png"));
                Assert.Equal(BinaryPrimitives.ReadInt32BigEndian(first.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
                Assert.Equal(BinaryPrimitives.ReadInt32BigEndian(first.AsSpan(20, 4)), BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
                Assert.Equal(6, png[25]);
            }
        }
    }
}
