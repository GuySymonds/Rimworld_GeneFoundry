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
        [InlineData(9,false)] [InlineData(10,true)] [InlineData(20,true)]
        public void Qualifications(int crafting,bool expected) => Assert.Equal(expected,FabricationCalculations.Qualified(crafting));
        [Theory]
        [InlineData(0,.3f,.5f)] [InlineData(10,.3f,1.5f)]
        [InlineData(15,.15f,2f)] [InlineData(20,0f,2.5f)] [InlineData(30,0f,2.5f)]
        public void CraftingControlsPerformance(int crafting,float risk,float speed)
        {
            Assert.Equal(risk,FabricationCalculations.Failure(crafting),5);
            Assert.Equal(speed,FabricationCalculations.Speed(crafting),5);
        }
        [Fact] public void ActualWorkWeightsRiskRatherThanLastWorker()
        {
            Assert.Equal(.27f,FabricationCalculations.WeightedFailure(90*.3f+10*0,100),5);
            Assert.Equal(.03f,FabricationCalculations.WeightedFailure(10*.3f+90*0,100),5);
            Assert.Equal(.30f,FabricationCalculations.WeightedFailure(0,0),5);
        }
    }
}
