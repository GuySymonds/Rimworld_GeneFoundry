using GeneFoundry.Recipes;
using GeneFoundry.Fabrication;
using Xunit;
namespace GeneFoundry.Tests
{
    public class LiquidationVisualTests
    {
        [Theory]
        [InlineData(100,100,0)] [InlineData(100,50,.5f)] [InlineData(100,0,1)]
        [InlineData(100,-20,1)] [InlineData(100,200,0)] [InlineData(0,0,0)]
        public void VisualProgressStillTracksExtraction(int total,int left,float progress)
            => Assert.Equal(progress,BiomassCalculations.LiquidationVisualProgress(total,left),5);

        [Theory]
        [InlineData(.4f,.2f,2,1,1)]
        [InlineData(2,1,2,1,1)]
        [InlineData(4,2,2,1,1)]
        [InlineData(4,1,2,1,1)]
        [InlineData(1,4,2,1,.5f)]
        [InlineData(2,4,2,1,.5f)]
        [InlineData(8,4,2,1,.5f)]
        [InlineData(0,0,2,1,1)]
        public void CorpseFitUsesOrientedDimensionsWithoutEnlargement(float width,float height,float basinWidth,float basinHeight,float expected)
        {
            float scale = BiomassCalculations.LiquidationVisualScale(width,height,basinWidth,basinHeight);
            Assert.Equal(expected,scale,5);
            Assert.InRange(scale,0f,1f);
            Assert.True(width * scale <= 2f * basinWidth && height * scale <= 2f * basinHeight);
            // The same factor on both axes preserves aspect ratio. No progress input.
            Assert.Equal(4,typeof(BiomassCalculations).GetMethod("LiquidationVisualScale").GetParameters().Length);
        }

        [Fact]
        public void BubbleMassLoopsForwardAtSixTicksPerFrame()
        {
            for (int tick = 0; tick < 360; tick++)
                Assert.Equal(tick / 6 % 8,BiomassCalculations.BubbleMassFrame(tick));
            Assert.Equal(7,BiomassCalculations.BubbleMassFrame(47));
            Assert.Equal(0,BiomassCalculations.BubbleMassFrame(48));
        }
        [Theory]
        [InlineData(0,0,0)] [InlineData(0,.34f,1)] [InlineData(0,.67f,2)]
        [InlineData(1,0,1)] [InlineData(2,.34f,2)] [InlineData(2,1,2)]
        public void VisualDecompositionNeverMakesAnAlreadyDecayedCorpseLookFresher(int actual,float progress,int visual)
            => Assert.Equal(visual,BiomassCalculations.LiquidationVisualStage(actual,progress));
        [Theory]
        [InlineData(0,0)] [InlineData(1,0)] [InlineData(2,1)] [InlineData(3,2)]
        [InlineData(4,3)] [InlineData(23,17)] [InlineData(100,75)]
        public void CancellationReturnsSeventyFivePercentRoundedDown(int paid,int recovered)
            => Assert.Equal(recovered,FabricationCalculations.CancellationRecovery(paid));
    }
}
