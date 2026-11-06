using GeneFoundry.Recipes;
using Xunit;
namespace GeneFoundry.Tests
{
    public class RefinementCalculationsTests
    {
        [Theory]
        [InlineData(360, 60, 20f, 15)]
        [InlineData(360, 120, 20f, 12)]
        [InlineData(360, 120, 0f, 0)]
        [InlineData(360, 360, 20f, 0)]
        [InlineData(360, 120, 25f, 10)]
        [InlineData(2160, 1080, 108f, 10)]
        public void ProcessorAllocationStopsOnlyWhenNoFurtherTimeCanBeSaved(int duration, int minimum, float reduction, int expected) =>
            Assert.Equal(expected, BiomassCalculations.RefinementUsefulProcessors(duration, minimum, reduction));

        [Theory]
        [InlineData(5,90,0.30f)] [InlineData(10,70,0.05f)]
        [InlineData(15,50,0.025f)] [InlineData(20,30,0f)]
        [InlineData(0,90,0.30f)] [InlineData(30,30,0f)]
        public void LinearPreparationAndPiecewiseFailure(int level,int minutes,float failure)
        {
            Assert.Equal((int)System.Math.Round(minutes*2500d/60d),
                BiomassCalculations.RefinementPreparationTicks(level));
            Assert.Equal(failure,BiomassCalculations.RefinementFailureChance(level),6);
        }
        [Theory]
        [InlineData(5, .30f)]
        [InlineData(7, .20f)]
        [InlineData(10, .05f)]

        [InlineData(12, .04f)]
        [InlineData(15, .025f)]
        [InlineData(20, 0f)]
        [InlineData(-10, .30f)]
        [InlineData(30, 0f)]
        public void FailureInterpolatesAtIntellectualAndClamps(int i, float expected) =>
            Assert.Equal(expected, BiomassCalculations.RefinementFailureChance(i), 6);

        [Theory]
        [InlineData(5,5d)] [InlineData(10,10d)]
        [InlineData(20,20d)]
        public void IntellectualClamps(int i,double expected) =>
            Assert.Equal(expected,BiomassCalculations.RefinementSkill(i),6);

        [Theory]
        [InlineData(5,0,false,.30f)] [InlineData(5,-1,false,.375f)]
        [InlineData(5,-2,false,.45f)] [InlineData(5,0,true,.40f)]
        [InlineData(10,0,false,.05f)] [InlineData(10,-1,false,.125f)]
        [InlineData(10,-2,false,.20f)] [InlineData(10,0,true,.15f)]
        [InlineData(20,0,false,0f)] [InlineData(20,-1,false,.075f)]
        [InlineData(20,-2,false,.15f)] [InlineData(20,0,true,.10f)]
        public void EnvironmentAddsRiskAtEverySkill(int skill,double cleanliness,bool exposed,float expected) =>
            Assert.Equal(expected,BiomassCalculations.CombinedRefinementFailure(
                BiomassCalculations.RefinementFailureChance(skill),
                BiomassCalculations.RefinementCleanlinessAdjustment(cleanliness,exposed)),6);
    }
}
