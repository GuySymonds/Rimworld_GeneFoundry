using GeneFoundry.Recipes;
using GeneFoundry.Fabrication;
using Xunit;
namespace GeneFoundry.Tests
{
    public class LiquidationVisualTests
    {
        [Theory]
        [InlineData(100,100,0,1)] [InlineData(100,50,.5f,.75f)] [InlineData(100,0,1,.5f)]
        [InlineData(100,-20,1,.5f)] [InlineData(100,200,0,1)] [InlineData(0,0,0,1)]
        public void CorpseProgressShrinksOnlyTheVisualToHalfSize(int total,int left,float progress,float scale)
        {
            Assert.Equal(progress,BiomassCalculations.LiquidationVisualProgress(total,left),5);
            Assert.Equal(scale,BiomassCalculations.LiquidationVisualScale(progress),5);
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
