using GeneFoundry.Fabrication;
using Xunit;
namespace GeneFoundry.Tests
{
    public class FabricationCalculationsTests
    {
        [Theory]
        [InlineData(1,0,2500)] [InlineData(1,2,4500)] [InlineData(3,5,12500)]
        public void WorkIsUnaffectedByBiomassDiscounts(int genes,int complexity,float work) =>
            Assert.Equal(work,FabricationCalculations.Work(genes,complexity));

        [Theory]
        [InlineData(new int[] { }, 0)]
        [InlineData(new[] { 0 }, 10)]
        [InlineData(new[] { 2 }, 20)]
        [InlineData(new[] { -5 }, 10)]
        [InlineData(new[] { 0, 0 }, 19)]
        [InlineData(new[] { 0, 1 }, 24)]
        [InlineData(new[] { 0, 0, 0 }, 27)]
        [InlineData(new[] { 0, 0, 0, 0 }, 34)]
        [InlineData(new[] { 0, 0, 0, 0, 0 }, 40)]
        [InlineData(new[] { 0, 0, 0, 0, 0, 0 }, 45)]
        [InlineData(new[] { 0, 0, 0, 0, 0, 0, 0 }, 50)]
        [InlineData(new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 60)]
        [InlineData(new[] { 5, 0, 0 }, 52)]
        [InlineData(new[] { 3, 2, 0 }, 51)]
        [InlineData(new[] { 0, 3, 2 }, 51)]
        [InlineData(new[] { 1, 1 }, 29)]
        [InlineData(new[] { 4, 3, 2, 1 }, 79)]
        public void BiomassPricesIndividualGenesByDescendingCostAndRoundsTotalOnce(int[] complexities,int expected) =>
            Assert.Equal(expected,FabricationCalculations.Biomass(complexities));
        [Theory]
        [InlineData(10,6,4,true)] [InlineData(9,20,20,false)] [InlineData(20,5,20,false)] [InlineData(20,20,3,false)]
        public void Qualifications(int i,int m,int c,bool expected) => Assert.Equal(expected,FabricationCalculations.Qualified(i,m,c));
        [Fact] public void RiskThresholdsAndProgression()
        {
            Assert.Equal(.30f,FabricationCalculations.Failure(10,6,4),5);
            Assert.Equal(.15f,FabricationCalculations.Failure(13,9,7),5);
            Assert.Equal(0,FabricationCalculations.Failure(16,12,10));
            Assert.Equal(0,FabricationCalculations.Failure(20,20,20));
            Assert.Equal(.30f,FabricationCalculations.Failure(0,0,0),5);
            Assert.Equal(.075f,FabricationCalculations.Failure(20,6,20),5);
        }
        [Fact] public void ActualWorkWeightsRiskRatherThanLastWorker()
        {
            Assert.Equal(.27f,FabricationCalculations.WeightedFailure(90*.3f+10*0,100),5);
            Assert.Equal(.03f,FabricationCalculations.WeightedFailure(10*.3f+90*0,100),5);
            Assert.Equal(.30f,FabricationCalculations.WeightedFailure(0,0),5);
        }
        [Fact] public void SpeedUsesAllThreeSkillsAndCapsIndependently()
        {
            Assert.Equal(1.31f,FabricationCalculations.Speed(10,6,4),5);
            Assert.True(FabricationCalculations.Speed(16,12,10)>FabricationCalculations.Speed(10,6,4));
            Assert.Equal(2.5f,FabricationCalculations.Speed(30,30,30),5);
        }
    }
}
