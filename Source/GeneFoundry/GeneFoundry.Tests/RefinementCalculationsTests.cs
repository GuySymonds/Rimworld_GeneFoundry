using GeneFoundry.Recipes;
using Xunit;
namespace GeneFoundry.Tests
{
    public class RefinementCalculationsTests
    {
        [Theory]
        [InlineData(5,90,0.30f)] [InlineData(10,70,0.20f)]
        [InlineData(15,50,0.10f)] [InlineData(20,30,0f)]
        [InlineData(0,90,0.30f)] [InlineData(30,30,0f)]
        public void LinearPreparationAndFailure(int level,int minutes,float failure)
        {
            Assert.Equal((int)System.Math.Round(minutes*2500d/60d),
                BiomassCalculations.RefinementPreparationTicks(level,level,level));
            Assert.Equal(failure,BiomassCalculations.RefinementFailureChance(level,level,level),6);
        }
        [Theory]
        [InlineData(5,3,2,5d)] [InlineData(10,8,6,8.9d)]
        [InlineData(20,3,2,13.05d)] [InlineData(20,20,20,20d)]
        public void SingleWeightedSkill(int i,int c,int m,double expected) =>
            Assert.Equal(expected,BiomassCalculations.RefinementSkill(i,c,m),6);

        [Theory]
        [InlineData(5,0,false,.30f)] [InlineData(5,-1,false,.375f)]
        [InlineData(5,-2,false,.45f)] [InlineData(5,0,true,.40f)]
        [InlineData(10,0,false,.20f)] [InlineData(10,-1,false,.275f)]
        [InlineData(10,-2,false,.35f)] [InlineData(10,0,true,.30f)]
        [InlineData(20,0,false,0f)] [InlineData(20,-1,false,.075f)]
        [InlineData(20,-2,false,.15f)] [InlineData(20,0,true,.10f)]
        public void EnvironmentAddsRiskAtEverySkill(int skill,double cleanliness,bool exposed,float expected) =>
            Assert.Equal(expected,BiomassCalculations.CombinedRefinementFailure(
                BiomassCalculations.RefinementFailureChance(skill,skill,skill),
                BiomassCalculations.RefinementCleanlinessAdjustment(cleanliness,exposed)),6);
    }
}
