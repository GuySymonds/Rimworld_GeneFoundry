using GeneFoundry.Fabrication;
using Xunit;
namespace GeneFoundry.Tests
{
    public class FabricationCalculationsTests
    {
        [Theory]
        [InlineData(1,0,10,2500)] [InlineData(1,2,20,4500)] [InlineData(3,5,44,12500)]
        public void Costs(int genes,int complexity,int biomass,float work)
        { Assert.Equal(biomass,FabricationCalculations.Biomass(genes,complexity)); Assert.Equal(work,FabricationCalculations.Work(genes,complexity)); }
        [Theory]
        [InlineData(1,0,10)]
        [InlineData(2,0,18)]
        [InlineData(2,1,23)]
        [InlineData(3,0,24)]
        [InlineData(4,0,28)]
        [InlineData(5,0,30)]
        [InlineData(6,0,30)]
        [InlineData(7,0,35)]
        [InlineData(10,1,53)]
        public void BiomassDiscountRoundsUpAndCapsAtFiftyPercent(int genes,int complexity,int cost)
        {
            Assert.Equal(cost,FabricationCalculations.Biomass(genes,complexity));
            Assert.Equal(System.Math.Min(50,(genes-1)*10),FabricationCalculations.BiomassDiscountPercent(genes));
            Assert.Equal(genes*2500f+complexity*1000f,FabricationCalculations.Work(genes,complexity));
        }
        [Fact] public void EmptyPackHasNoDiscountAndSingleGeneCostsAreUnchanged()
        {
            Assert.Equal(0,FabricationCalculations.Biomass(0,0));
            Assert.Equal(0,FabricationCalculations.BiomassDiscountPercent(0));
            Assert.Equal(0,FabricationCalculations.BiomassDiscountPercent(-1));
            Assert.Equal(50,FabricationCalculations.BiomassDiscountPercent(int.MaxValue));
            Assert.Equal(60,FabricationCalculations.Biomass(1,10));
        }
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
